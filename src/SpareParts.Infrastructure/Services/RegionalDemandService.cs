using Dapper;
using SpareParts.Domain.Analytics;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class RegionalDemandService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public RegionalDemandService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<RegionalDemandDto> GetByRegion(string? partName = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var rows = session.Connection.Query<RegionalDemandRow>(
            """
SELECT
    w.LocationCountry AS Region,
    w.LocationCity AS City,
    w.PartName,
    COUNT(1) AS DemandCount
FROM dbo.PartWantedAds w
WHERE (@TenantId = 0 OR w.TenantId = @TenantId)
  AND w.Status = 1
  AND (@PartName IS NULL OR w.PartName LIKE '%' + @PartName + '%')
GROUP BY w.LocationCountry, w.LocationCity, w.PartName
ORDER BY DemandCount DESC;
""",
            new { TenantId = _tenantContext.TenantId, PartName = partName },
            session.Transaction).ToList();

        var partNames = rows.Select(r => r.PartName).Distinct().ToList();
        var supplyCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in partNames)
        {
            var count = session.Connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.Parts WHERE IsActive = 1 AND SalePrice > 0 AND Name LIKE '%' + @Name + '%';",
                new { Name = name },
                session.Transaction);
            supplyCounts[name] = count;
        }

        return rows.Select(r =>
        {
            var supply = supplyCounts.GetValueOrDefault(r.PartName, 0);
            return new RegionalDemandDto
            {
                Region = r.Region,
                City = r.City,
                PartName = r.PartName,
                DemandCount = r.DemandCount,
                SupplyCount = supply,
                InsightLabel = r.DemandCount > supply * 2 ? "High Demand" : r.DemandCount > supply ? "Moderate Demand" : "Balanced"
            };
        });
    }

    public IEnumerable<PartDemandInsightDto> GetTopDemandedParts(int top = 20)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<PartDemandInsightDto>(
            """
SELECT TOP (@Top)
    w.PartName,
    COUNT(1) AS TotalDemand,
    (SELECT COUNT(1) FROM dbo.Parts p2 WHERE p2.IsActive = 1 AND p2.SalePrice > 0 AND p2.Name LIKE '%' + w.PartName + '%') AS ActiveListings,
    COUNT(1) - (SELECT COUNT(1) FROM dbo.Parts p2 WHERE p2.IsActive = 1 AND p2.SalePrice > 0 AND p2.Name LIKE '%' + w.PartName + '%') AS Gap,
    CASE
        WHEN COUNT(1) > 5 THEN 'Hot'
        WHEN COUNT(1) > 2 THEN 'Warm'
        ELSE 'Cool'
    END AS InsightLabel
FROM dbo.PartWantedAds w
WHERE (@TenantId = 0 OR w.TenantId = @TenantId) AND w.Status = 1
GROUP BY w.PartName
ORDER BY TotalDemand DESC;
""",
            new { Top = top, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }
}
