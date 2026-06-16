using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class MarketPriceIndexService
{
    private readonly ISqlConnectionFactory _factory;

    public MarketPriceIndexService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public MarketPriceIndexDto? GetIndex(string partName, string? make = null, string? model = null, int? year = null)
    {
        if (string.IsNullOrWhiteSpace(partName))
            throw new ValidationException("Part name is required.");

        using var connection = _factory.CreateConnection();

        var row = connection.QuerySingleOrDefault<PriceIndexRow>(
            """
SELECT
    MIN(SalePrice) AS MinPrice,
    MAX(SalePrice) AS MaxPrice,
    AVG(SalePrice) AS AveragePrice,
    COUNT(1) AS SampleCount,
    MIN(Currency) AS Currency
FROM dbo.Parts
WHERE IsActive = 1
  AND SalePrice > 0
  AND Name LIKE '%' + @PartName + '%';
""",
            new { PartName = partName.Trim() });

        if (row is null || row.SampleCount == 0)
            return null;

        return new MarketPriceIndexDto
        {
            PartName = partName.Trim(),
            MinPrice = row.MinPrice,
            MaxPrice = row.MaxPrice,
            AveragePrice = row.AveragePrice,
            SampleCount = row.SampleCount,
            Currency = row.Currency ?? "USD",
            BasedOn = "active_listings",
            ComputedAt = DateTime.UtcNow
        };
    }
}
