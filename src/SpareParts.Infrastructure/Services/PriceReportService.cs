using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Reports;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PriceReportService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PriceReportService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public PriceReportDto Generate(PriceReportFilter filter)
    {
        if (string.IsNullOrWhiteSpace(filter.PartName))
            throw new ValidationException("Part name is required.");

        using var connection = _factory.CreateConnection();

        var row = connection.QuerySingleOrDefault<PriceReportRow>(
            """
SELECT
    @PartName AS PartName,
    MIN(SalePrice) AS MinPrice,
    MAX(SalePrice) AS MaxPrice,
    AVG(SalePrice) AS AvgPrice,
    COUNT(1) AS SampleCount,
    MIN(Currency) AS Currency
FROM dbo.Parts
WHERE IsActive = 1
  AND SalePrice > 0
  AND Name LIKE '%' + @PartName + '%'
  AND (@Make IS NULL OR EXISTS (
      SELECT 1 FROM dbo.PartCompatibilities pc
      WHERE pc.PartId = dbo.Parts.Id
        AND pc.CompatibleMakes LIKE '%' + @Make + '%'
  ))
  AND (@FromDate IS NULL OR CreatedAt >= @FromDate)
  AND (@ToDate IS NULL OR CreatedAt <= @ToDate);
""",
            new
            {
                PartName = filter.PartName.Trim(),
                Make = filter.Make,
                FromDate = filter.FromDate,
                ToDate = filter.ToDate
            });

        var trend = DetermineTrend(filter.PartName, connection);

        return new PriceReportDto
        {
            PartName = filter.PartName.Trim(),
            MinPrice = row?.MinPrice,
            MaxPrice = row?.MaxPrice,
            AvgPrice = row?.AvgPrice,
            SampleCount = row?.SampleCount ?? 0,
            TrendDirection = trend,
            Currency = row?.Currency ?? "USD",
            GeneratedAt = DateTime.UtcNow
        };
    }

    private static string DetermineTrend(string partName, System.Data.IDbConnection connection)
    {
        var recentAvg = connection.ExecuteScalar<decimal?>(
            """
SELECT AVG(SalePrice)
FROM dbo.Parts
WHERE IsActive = 1 AND SalePrice > 0
  AND Name LIKE '%' + @PartName + '%'
  AND CreatedAt >= DATEADD(DAY, -30, GETUTCDATE());
""",
            new { PartName = partName });

        var olderAvg = connection.ExecuteScalar<decimal?>(
            """
SELECT AVG(SalePrice)
FROM dbo.Parts
WHERE IsActive = 1 AND SalePrice > 0
  AND Name LIKE '%' + @PartName + '%'
  AND CreatedAt < DATEADD(DAY, -30, GETUTCDATE())
  AND CreatedAt >= DATEADD(DAY, -90, GETUTCDATE());
""",
            new { PartName = partName });

        if (!recentAvg.HasValue || !olderAvg.HasValue || olderAvg.Value == 0)
            return "Stable";

        var change = (recentAvg.Value - olderAvg.Value) / olderAvg.Value;
        return change > 0.05m ? "Rising" : change < -0.05m ? "Falling" : "Stable";
    }
}
