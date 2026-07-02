using System.Text;
using Dapper;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Backs the Marketing Agent. Finds parts the Pricing Agent just activated, matches them by name
/// against open customer part requests and need-board "wanted" ads, and sends each matched
/// customer a "the part you wanted is now available" WhatsApp message via
/// <see cref="CommunicationsService"/>. Each part is only ever scanned once (tracked via
/// <c>MarketingNotifiedAt</c>), regardless of whether it found zero, one, or many matches.
/// </summary>
public sealed class DemandMatchingService
{
    /// <summary>Escape character used in LIKE patterns built from user/catalog-controlled text.</summary>
    private const char LikeEscapeChar = '\\';

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly CommunicationsService _communicationsService;

    public DemandMatchingService(
        ISqlConnectionFactory factory,
        ITenantContext tenantContext,
        CommunicationsService communicationsService)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _communicationsService = communicationsService;
    }

    public IReadOnlyList<PartPendingMarketingDto> GetPartsPendingMarketing()
    {
        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        return conn.Query<PartPendingMarketingDto>(
            @"SELECT p.Id AS PartId,
                     p.Name,
                     p.SalePrice,
                     p.Currency,
                     ISNULL(uc.CreatedByUserId, 0) AS CreatedByUserId
              FROM dbo.Parts p
              LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
              WHERE p.PricingStatus = 'Calculated'
                AND p.IsActive = 1
                AND p.MarketingNotifiedAt IS NULL
                AND (@TenantId = 0 OR p.TenantId = @TenantId)
              ORDER BY p.Id;",
            new { TenantId = tenantId }).ToList();
    }

    /// <summary>
    /// Name-matches a single part against open part requests and need-board ads. Prefer
    /// <see cref="FindMatchesForParts"/> when matching several parts at once, since this method
    /// issues its own pair of SQL round-trips.
    /// </summary>
    public IReadOnlyList<DemandMatchDto> FindMatches(string partName)
    {
        var byPartName = FindMatchesForParts([partName]);
        return byPartName.TryGetValue(partName, out var matches)
            ? matches
            : Array.Empty<DemandMatchDto>();
    }

    /// <summary>
    /// Batched version of <see cref="FindMatches(string)"/>: scans open part requests and
    /// need-board ads exactly once (one query per source) for every part name supplied, instead of
    /// two SQL round-trips per part. Vehicle-compatibility-based matching isn't used here since
    /// that data isn't populated for freshly torn-down parts. Returns matches keyed by the exact
    /// part name passed in; a part name with no matches is present in the result with an empty list.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<DemandMatchDto>> FindMatchesForParts(
        IReadOnlyList<string> partNames)
    {
        var result = new Dictionary<string, IReadOnlyList<DemandMatchDto>>(StringComparer.Ordinal);
        var distinctNames = partNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var name in distinctNames)
        {
            result[name] = new List<DemandMatchDto>();
        }

        if (distinctNames.Count == 0)
        {
            return result;
        }

        using var conn = _factory.CreateConnection();

        var (partRequestWhere, parameters) = BuildLikeAnyClause("pr.RequestedPartName", distinctNames);
        var partRequestRows = conn.Query<PartNameDemandMatchRow>(
            $@"SELECT pr.RequestedPartName AS PartName,
                      pr.Id AS SourceId,
                      'PartRequest' AS SourceType,
                      pr.CustomerName AS Name,
                      pr.CustomerPhone AS Phone
               FROM dbo.PartRequests pr
               WHERE pr.Status IN ('Open', 'Contacted')
                 AND pr.CustomerPhone IS NOT NULL
                 AND LTRIM(RTRIM(pr.CustomerPhone)) <> ''
                 AND ({partRequestWhere});",
            parameters);

        var (needBoardWhere, needBoardParameters) = BuildLikeAnyClause("ad.PartName", distinctNames);
        var needBoardRows = conn.Query<PartNameDemandMatchRow>(
            $@"SELECT ad.PartName AS PartName,
                      ad.Id AS SourceId,
                      'NeedBoard' AS SourceType,
                      ad.BuyerName AS Name,
                      ad.BuyerPhone AS Phone
               FROM dbo.PartWantedAds ad
               WHERE ad.Status IN (1, 2)
                 AND ad.BuyerPhone IS NOT NULL
                 AND LTRIM(RTRIM(ad.BuyerPhone)) <> ''
                 AND ({needBoardWhere});",
            needBoardParameters);

        AppendMatches(result, partRequestRows, distinctNames);
        AppendMatches(result, needBoardRows, distinctNames);

        return result;
    }

    public void MarkNotified(int partId)
    {
        using var conn = _factory.CreateConnection();
        conn.Execute(
            "UPDATE dbo.Parts SET MarketingNotifiedAt = SYSUTCDATETIME() WHERE Id = @PartId;",
            new { PartId = partId });
    }

    public async Task NotifyMatchAsync(int partId, DemandMatchDto match, int userId, CancellationToken cancellationToken)
    {
        var request = new SendBusinessMessageRequest
        {
            Channel = CommunicationChannel.WhatsApp,
            TemplateKey = CommunicationTemplateKey.PartAvailability,
            RecipientKind = CommunicationRecipientKind.Manual,
            RecipientPhoneOverride = match.Phone,
            RecipientNameOverride = match.Name,
            PartId = partId,
            Note = match.SourceType == "PartRequest"
                ? "Matched from your part request."
                : "Matched from your wanted ad on the need board."
        };

        await _communicationsService.SendAsync(request, userId, cancellationToken);
    }

    /// <summary>
    /// Builds a single "column LIKE @p0 ESCAPE '\' OR column LIKE @p1 ESCAPE '\' OR ..." clause
    /// covering every supplied part name in one query, with each name escaped so that literal
    /// <c>%</c>, <c>_</c>, and <c>[</c> characters in a part name cannot alter the wildcard pattern.
    /// </summary>
    private static (string Where, DynamicParameters Parameters) BuildLikeAnyClause(
        string columnName,
        IReadOnlyList<string> partNames)
    {
        var clause = new StringBuilder();
        var parameters = new DynamicParameters();

        for (var i = 0; i < partNames.Count; i++)
        {
            if (i > 0)
            {
                clause.Append(" OR ");
            }

            var paramName = $"PartName{i}";
            clause.Append(columnName)
                .Append(" LIKE '%' + @")
                .Append(paramName)
                .Append(" + '%' ESCAPE '")
                .Append(LikeEscapeChar)
                .Append('\'');

            parameters.Add(paramName, EscapeLikePattern(partNames[i]));
        }

        return (clause.ToString(), parameters);
    }

    /// <summary>
    /// Escapes SQL <c>LIKE</c> wildcard characters (<c>%</c>, <c>_</c>, <c>[</c>) and the escape
    /// character itself so that a part name containing them is matched literally, not as a
    /// wildcard pattern. Must be paired with an <c>ESCAPE '\'</c> clause on the LIKE expression.
    /// </summary>
    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeChar.ToString(), LikeEscapeChar.ToString() + LikeEscapeChar, StringComparison.Ordinal)
            .Replace("%", LikeEscapeChar + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeChar + "_", StringComparison.Ordinal)
            .Replace("[", LikeEscapeChar + "[", StringComparison.Ordinal);
    }

    private static void AppendMatches(
        Dictionary<string, IReadOnlyList<DemandMatchDto>> result,
        IEnumerable<PartNameDemandMatchRow> rows,
        IReadOnlyList<string> partNames)
    {
        foreach (var row in rows)
        {
            foreach (var name in partNames)
            {
                if (row.PartName.Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    ((List<DemandMatchDto>)result[name]).Add(new DemandMatchDto
                    {
                        SourceId = row.SourceId,
                        SourceType = row.SourceType,
                        Name = row.Name,
                        Phone = row.Phone
                    });
                }
            }
        }
    }
}
