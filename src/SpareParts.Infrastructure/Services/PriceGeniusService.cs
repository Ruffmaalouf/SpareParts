using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PriceGeniusService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly MarketPriceIndexService _marketPriceIndex;

    public PriceGeniusService(ISqlConnectionFactory factory, ITenantContext tenantContext, MarketPriceIndexService marketPriceIndex)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _marketPriceIndex = marketPriceIndex;
    }

    public PriceGeniusSuggestion Suggest(PriceGeniusRequest request)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var row = session.Connection.QuerySingleOrDefault<PartPriceRow>(
            """
SELECT p.Id, p.Name, p.SalePrice, p.Currency
FROM dbo.Parts p
WHERE p.Id = @PartId AND (@TenantId = 0 OR p.TenantId = @TenantId);
""",
            new { PartId = request.PartId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (row is null)
            throw new NotFoundException($"Part {request.PartId} not found.");

        var currency = request.OverrideCurrency ?? row.Currency ?? "USD";
        var index = _marketPriceIndex.GetIndex(row.Name);

        var result = new PriceGeniusSuggestion
        {
            PartId = request.PartId,
            PartName = row.Name,
            Currency = currency,
            IsProFeature = true,
        };

        if (index is null || index.SampleCount < 2)
        {
            result.IsLocked = false;
            result.Reasoning = "Insufficient market data. Set your own price.";
            result.SampleCount = index?.SampleCount ?? 0;
            return result;
        }

        var avg = index.AveragePrice ?? row.SalePrice;
        result.MarketAverage = avg;
        result.SampleCount = index.SampleCount;
        result.SuggestedRegularPrice = Math.Round(avg, 2);
        result.SuggestedFastSalePrice = Math.Round(avg * 0.85m, 2);
        result.SuggestedMaxProfitPrice = Math.Round(avg * 1.15m, 2);
        result.SuggestedMinPrice = Math.Round(avg * 0.70m, 2);
        result.Reasoning = $"Based on {index.SampleCount} active listings with avg {avg:F2} {currency}. FastSale=−15%, MaxProfit=+15%, Floor=−30%.";

        return result;
    }
}
