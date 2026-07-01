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
    /// Name-matches against open part requests and need-board ads. Vehicle-compatibility-based
    /// matching isn't used here since that data isn't populated for freshly torn-down parts.
    /// </summary>
    public IReadOnlyList<DemandMatchDto> FindMatches(string partName)
    {
        using var conn = _factory.CreateConnection();
        var matches = new List<DemandMatchDto>();

        matches.AddRange(conn.Query<DemandMatchDto>(
            @"SELECT TOP (20)
                     pr.Id AS SourceId,
                     'PartRequest' AS SourceType,
                     pr.CustomerName AS Name,
                     pr.CustomerPhone AS Phone
              FROM dbo.PartRequests pr
              WHERE pr.Status IN ('Open', 'Contacted')
                AND pr.RequestedPartName LIKE '%' + @PartName + '%'
                AND pr.CustomerPhone IS NOT NULL
                AND LTRIM(RTRIM(pr.CustomerPhone)) <> '';",
            new { PartName = partName }));

        matches.AddRange(conn.Query<DemandMatchDto>(
            @"SELECT TOP (20)
                     ad.Id AS SourceId,
                     'NeedBoard' AS SourceType,
                     ad.BuyerName AS Name,
                     ad.BuyerPhone AS Phone
              FROM dbo.PartWantedAds ad
              WHERE ad.Status IN (1, 2)
                AND ad.PartName LIKE '%' + @PartName + '%'
                AND ad.BuyerPhone IS NOT NULL
                AND LTRIM(RTRIM(ad.BuyerPhone)) <> '';",
            new { PartName = partName }));

        return matches;
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
}
