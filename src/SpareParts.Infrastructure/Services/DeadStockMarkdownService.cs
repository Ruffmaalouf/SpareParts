using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Backs the Dead Stock Markdown Agent. Finds parts that have been active and unsold for a long
/// time and steps their price down the same ladder the Pricing Agent already computed for them
/// (RecommendedPrice/current SalePrice → FastSalePrice → MinimumSellPrice), never going below the
/// floor. A part naturally stops being a candidate once its SalePrice reaches MinimumSellPrice, so
/// no extra "already marked down" tracking is needed beyond the audit timestamp.
/// </summary>
public sealed class DeadStockMarkdownService
{
    private const int FastSaleThresholdDays = 60;
    private const int FloorThresholdDays = 120;

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public DeadStockMarkdownService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<MarkdownCandidateDto> GetMarkdownCandidates()
    {
        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        return conn.Query<MarkdownCandidateDto>(
            @"SELECT p.Id AS PartId,
                     p.Name,
                     p.SalePrice,
                     p.FastSalePrice,
                     p.MinimumSellPrice,
                     p.PricingCalculatedAt
              FROM dbo.Parts p
              INNER JOIN (
                  SELECT PartId, SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
                  FROM dbo.Stock
                  GROUP BY PartId
              ) stock ON stock.PartId = p.Id
              WHERE p.PricingStatus = 'Calculated'
                AND p.IsActive = 1
                AND stock.AvailableQuantity > 0
                AND p.PricingCalculatedAt IS NOT NULL
                AND p.PricingCalculatedAt <= DATEADD(DAY, -@FastSaleThresholdDays, SYSUTCDATETIME())
                AND p.FastSalePrice > 0
                AND p.SalePrice > p.MinimumSellPrice
                AND (@TenantId = 0 OR p.TenantId = @TenantId);",
            new { TenantId = tenantId, FastSaleThresholdDays }).ToList();
    }

    /// <summary>
    /// Applies the next markdown step for <paramref name="part"/> if it's due, based on how long
    /// it's been live. Returns false (no DB write) if no step is due yet.
    /// </summary>
    public bool ApplyMarkdownIfDue(MarkdownCandidateDto part, out decimal? newPrice)
    {
        newPrice = null;
        if (part.PricingCalculatedAt is null) return false;

        var daysLive = (DateTime.UtcNow - part.PricingCalculatedAt.Value).TotalDays;

        decimal target;
        if (daysLive >= FloorThresholdDays && part.MinimumSellPrice > 0 && part.SalePrice > part.MinimumSellPrice)
        {
            target = part.MinimumSellPrice;
        }
        else if (daysLive >= FastSaleThresholdDays && part.FastSalePrice > 0 && part.SalePrice > part.FastSalePrice)
        {
            target = part.FastSalePrice;
        }
        else
        {
            return false;
        }

        using var conn = _factory.CreateConnection();
        conn.Execute(
            @"UPDATE dbo.Parts
              SET SalePrice = @Target,
                  LastMarkdownAppliedAt = SYSUTCDATETIME(),
                  ModifiedAt = SYSUTCDATETIME()
              WHERE Id = @PartId;",
            new { Target = target, PartId = part.PartId });

        newPrice = target;
        return true;
    }
}
