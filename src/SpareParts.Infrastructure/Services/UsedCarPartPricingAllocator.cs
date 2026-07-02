using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

internal static class UsedCarPartPricingAllocator
{
    public static void RepriceUsedCarParts(DbSession session, int? usedCarId, int? userId)
    {
        if (usedCarId is not > 0)
        {
            return;
        }

        var car = session.Connection.QuerySingleOrDefault<UsedCarPricingCarRow>(
            """
SELECT Id,
       GrandTotalBase,
       ExpectedSellThroughRate,
       BaseCurrencyCode,
       CounterCurrencyCode,
       CounterRateToBase
FROM dbo.UsedCars
WHERE Id = @UsedCarId;
""",
            new { UsedCarId = usedCarId.Value },
            session.Transaction);

        if (car == null)
        {
            return;
        }

        var parts = session.Connection.Query<UsedCarPricingPartRow>(
            """
SELECT Id AS PartId,
       EstimatedMarketPrice,
       AveragePrice,
       SalePrice,
       Currency
FROM dbo.Parts
WHERE UsedCarId = @UsedCarId
  AND IsActive = 1;
""",
            new { UsedCarId = usedCarId.Value },
            session.Transaction)
            .ToList();

        if (parts.Count == 0)
        {
            return;
        }

        var result = UsedVehiclePartPricingEngine.Calculate(
            car.GrandTotalBase,
            car.ExpectedSellThroughRate,
            parts.Select(part => new UsedVehiclePartPricingInput(
                part.PartId,
                part.EstimatedMarketPrice,
                part.AveragePrice,
                part.SalePrice,
                ResolvePartRateToBase(session, car, part.Currency))));

        foreach (var row in result.Rows)
        {
            session.Connection.Execute(
                """
UPDATE dbo.Parts
SET EstimatedMarketPrice = CASE
        WHEN @ExpectedMarketPrice > 0 THEN @ExpectedMarketPrice
        ELSE EstimatedMarketPrice
    END,
    CostAllocationPercent = @CostAllocationPercent,
    AllocatedCost = @AllocatedCost,
    MinimumSellPrice = @MinimumSellPrice,
    FastSalePrice = @FastSalePrice,
    WholesalePrice = @WholesalePrice,
    RecommendedPrice = @RecommendedPrice,
    PricingStatus = @PricingStatus,
    PricingCalculatedAt = @PricingCalculatedAt,
    CostPrice = CASE
        WHEN @AllocatedCost > 0 THEN @AllocatedCost
        ELSE CostPrice
    END,
    SalePrice = CASE
        WHEN ISNULL(SalePrice, 0) <= 0 AND @RecommendedPrice > 0 THEN @RecommendedPrice
        ELSE SalePrice
    END,
    ModifiedAt = @PricingCalculatedAt,
    ModifiedByUserId = @UserId
WHERE Id = @PartId;
""",
                new
                {
                    row.PartId,
                    row.ExpectedMarketPrice,
                    row.CostAllocationPercent,
                    row.AllocatedCost,
                    row.MinimumSellPrice,
                    row.FastSalePrice,
                    row.WholesalePrice,
                    row.RecommendedPrice,
                    row.PricingStatus,
                    PricingCalculatedAt = DateTime.UtcNow,
                    UserId = userId
                },
                session.Transaction);
        }
    }

    public static void ClearPartAllocation(DbSession session, int partId, int? userId)
    {
        session.Connection.Execute(
            """
UPDATE dbo.Parts
SET CostAllocationPercent = 0,
    AllocatedCost = 0,
    MinimumSellPrice = 0,
    FastSalePrice = 0,
    WholesalePrice = 0,
    RecommendedPrice = 0,
    PricingStatus = N'Manual',
    PricingCalculatedAt = NULL,
    ModifiedAt = @Now,
    ModifiedByUserId = @UserId
WHERE Id = @PartId;
""",
            new
            {
                PartId = partId,
                Now = DateTime.UtcNow,
                UserId = userId
            },
            session.Transaction);
    }

    private static decimal ResolvePartRateToBase(
        DbSession session,
        UsedCarPricingCarRow car,
        string? partCurrency)
    {
        var baseCurrencyCode = NormalizeCurrencyCode(car.BaseCurrencyCode) ?? "USD";
        var counterCurrencyCode = NormalizeCurrencyCode(car.CounterCurrencyCode) ?? baseCurrencyCode;
        var currencyCode = NormalizeCurrencyCode(partCurrency) ?? counterCurrencyCode;
        var counterRateToBase = car.CounterRateToBase > 0m ? car.CounterRateToBase : 1m;

        var rateToBase = AccountingCurrencyContextResolver.ResolveRateToBaseCurrency(
            session,
            baseCurrencyCode,
            currencyCode,
            counterCurrencyCode,
            counterRateToBase);

        return rateToBase > 0m ? rateToBase : 1m;
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        return normalized.Length == 3 ? normalized : null;
    }
}
