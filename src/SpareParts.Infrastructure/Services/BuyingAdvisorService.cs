using System.Globalization;
using Dapper;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Backs the Buying Advisor Agent. Rolls up which donor vehicle makes/models are selling fast
/// versus sitting as dead stock, and sends the shop owner a WhatsApp digest recommending what to
/// prioritize buying next — directly serving the one job the owner does by hand (buying cars).
///
/// The owner's WhatsApp number and the last-sent timestamp are both stored as generic
/// <c>AppConstants</c> key/value rows (no schema change) since this codebase has no existing
/// "owner"/"admin contact" concept. Ralph needs to set the "OwnerWhatsAppPhone" app constant once
/// for this agent to actually deliver its digest; until then it logs a warning and skips sending.
/// </summary>
public sealed class BuyingAdvisorService
{
    public const string OwnerPhoneConstantKey = "OwnerWhatsAppPhone";
    private const string LastDigestSentConstantKey = "BuyingAdvisorLastDigestSentAt";
    private const int MinHoursBetweenDigests = 20;

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly AppConstantsService _appConstantsService;
    private readonly CommunicationsService _communicationsService;

    public BuyingAdvisorService(
        ISqlConnectionFactory factory,
        ITenantContext tenantContext,
        AppConstantsService appConstantsService,
        CommunicationsService communicationsService)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _appConstantsService = appConstantsService;
        _communicationsService = communicationsService;
    }

    public string? GetOwnerPhone()
    {
        var value = _appConstantsService.GetAll()
            .FirstOrDefault(c => string.Equals(c.Key, OwnerPhoneConstantKey, StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool IsDigestDue()
    {
        var lastSent = _appConstantsService.GetAll()
            .FirstOrDefault(c => string.Equals(c.Key, LastDigestSentConstantKey, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(lastSent)) return true;

        if (!DateTime.TryParse(lastSent, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastSentAt))
        {
            return true;
        }

        return (DateTime.UtcNow - lastSentAt.ToUniversalTime()).TotalHours >= MinHoursBetweenDigests;
    }

    public void RecordDigestSent()
        => _appConstantsService.Upsert(LastDigestSentConstantKey, new UpsertAppConstantRequest
        {
            Value = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Description = "Set automatically by the Buying Advisor Agent."
        });

    public IReadOnlyList<DonorVehiclePerformanceDto> GetDonorVehiclePerformance()
    {
        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        return conn.Query<DonorVehiclePerformanceDto>(
            @"WITH SoldByCar AS (
                  SELECT p.UsedCarId, SUM(ABS(ISNULL(ti.Quantity, 0))) AS SoldQty
                  FROM dbo.TransactionItems ti
                  INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = 'Sale'
                  INNER JOIN dbo.Parts p ON p.Id = ti.PartId
                  WHERE ISNULL(t.IsReturn, 0) = 0
                    AND t.TransactionDate >= DATEADD(DAY, -90, SYSUTCDATETIME())
                    AND p.UsedCarId IS NOT NULL
                  GROUP BY p.UsedCarId
              ),
              DeadByCar AS (
                  SELECT p.UsedCarId, COUNT(1) AS DeadPartCount
                  FROM dbo.Parts p
                  INNER JOIN (
                      SELECT PartId, SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
                      FROM dbo.Stock
                      GROUP BY PartId
                  ) stock ON stock.PartId = p.Id
                  WHERE p.IsActive = 1
                    AND p.UsedCarId IS NOT NULL
                    AND stock.AvailableQuantity > 0
                    AND p.PricingCalculatedAt IS NOT NULL
                    AND p.PricingCalculatedAt <= DATEADD(DAY, -90, SYSUTCDATETIME())
                  GROUP BY p.UsedCarId
              )
              SELECT cb.Name AS Make,
                     cm.Name AS Model,
                     SUM(ISNULL(sold.SoldQty, 0)) AS SoldQty90d,
                     SUM(ISNULL(dead.DeadPartCount, 0)) AS DeadPartCount
              FROM dbo.UsedCars uc
              INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
              INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
              LEFT JOIN SoldByCar sold ON sold.UsedCarId = uc.Id
              LEFT JOIN DeadByCar dead ON dead.UsedCarId = uc.Id
              WHERE (@TenantId = 0 OR uc.TenantId = @TenantId)
              GROUP BY cb.Name, cm.Name
              HAVING SUM(ISNULL(sold.SoldQty, 0)) > 0 OR SUM(ISNULL(dead.DeadPartCount, 0)) > 0
              ORDER BY SoldQty90d DESC;",
            new { TenantId = tenantId }).ToList();
    }

    public string ComposeDigest(IReadOnlyList<DonorVehiclePerformanceDto> performance)
    {
        var topSellers = performance.Where(p => p.SoldQty90d > 0).OrderByDescending(p => p.SoldQty90d).Take(3).ToList();
        var slowMovers = performance.Where(p => p.DeadPartCount > 0).OrderByDescending(p => p.DeadPartCount).Take(3).ToList();

        var lines = new List<string>
        {
            "Weekly buying advisor update from your SpareParts shop:",
            topSellers.Count > 0
                ? "Selling fast, consider buying more: " + string.Join(", ", topSellers.Select(p => $"{p.Make} {p.Model} ({p.SoldQty90d} parts sold in 90 days)"))
                : "No strong sellers yet in the last 90 days.",
            slowMovers.Count > 0
                ? "Sitting unsold 90+ days, consider pausing on these: " + string.Join(", ", slowMovers.Select(p => $"{p.Make} {p.Model} ({p.DeadPartCount} parts unsold)"))
                : "No significant dead stock right now."
        };

        return string.Join(Environment.NewLine, lines);
    }

    public async Task SendDigestAsync(string ownerPhone, string body, int userId, CancellationToken cancellationToken)
    {
        var request = new SendBusinessMessageRequest
        {
            Channel = CommunicationChannel.WhatsApp,
            TemplateKey = CommunicationTemplateKey.FreeText,
            RecipientKind = CommunicationRecipientKind.Manual,
            RecipientPhoneOverride = ownerPhone,
            RecipientNameOverride = "Shop Owner",
            MessageBody = body
        };

        await _communicationsService.SendAsync(request, userId, cancellationToken);
    }
}
