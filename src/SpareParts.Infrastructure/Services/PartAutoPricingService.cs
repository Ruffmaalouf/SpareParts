using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Backs the Pricing Agent. Finds draft parts the Intake &amp; Teardown Agent created that still
/// need a price, and prices an entire used car's batch of draft parts in one pass using the same
/// cost-allocation engine (<see cref="UsedVehiclePartPricingEngine"/>) already used elsewhere in
/// the app — seeded with real comparable prices where they exist, or a relative-value heuristic
/// when they don't.
///
/// Design note: this only ever prices a car's parts ONCE, right after they're created as drafts
/// (while all of them are still unsold and unpriced). It intentionally does not re-run allocation
/// later, because the underlying engine proportionally splits the vehicle's total cost across
/// whatever parts are in the batch — re-running it after some parts have already sold would
/// silently reshuffle pricing for those already-sold siblings too. Once a part is priced here its
/// <c>PricingStatus</c> moves off <c>"Manual"</c> and it is never picked up again.
/// </summary>
public sealed class PartAutoPricingService
{
    // Bootstrap weighting used only when no real comparable listings exist yet for a part name.
    // As the shop processes more cars, MarketPriceIndexService comparables take over for any part
    // name that has now sold/listed enough times, and this fallback stops being used for it.
    private const decimal HighValueFastMovingWeight = 6m;
    private const decimal HighValueWeight = 5m;
    private const decimal FastMovingWeight = 2m;
    private const decimal BaseWeight = 1m;

    // A parted-out vehicle's combined retail part value is typically well above its landed cost
    // (that spread is the shop's margin). This multiplier only shapes the STARTING dollar figure
    // fed into the allocation engine when there's no market comparable yet — the engine's own
    // cost-allocation math (tied to the vehicle's real recorded cost) is what actually drives the
    // final AllocatedCost/RecommendedPrice, so a rough multiplier here is a safe, correctable seed.
    private const decimal EstimatedRetailValueMultiplier = 2.2m;

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly MarketPriceIndexService _marketPriceIndex;

    public PartAutoPricingService(
        ISqlConnectionFactory factory,
        ITenantContext tenantContext,
        MarketPriceIndexService marketPriceIndex)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _marketPriceIndex = marketPriceIndex;
    }

    public IReadOnlyList<PendingPricingPartDto> GetPendingPricingParts()
    {
        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        return conn.Query<PendingPricingPartDto>(
            @"SELECT p.Id AS PartId,
                     p.Name,
                     p.UsedCarId
              FROM dbo.Parts p
              WHERE p.PricingStatus = 'Manual'
                AND p.UsedCarId IS NOT NULL
                AND (@TenantId = 0 OR p.TenantId = @TenantId)
              ORDER BY p.UsedCarId, p.Id;",
            new { TenantId = tenantId }).ToList();
    }

    /// <summary>
    /// Prices every part in <paramref name="parts"/> (all belonging to <paramref name="usedCarId"/>)
    /// in a single allocation pass. Returns the number of parts successfully priced and activated.
    /// Automated changes are attributed to the owner who originally logged the used car purchase,
    /// consistent with the Intake &amp; Teardown Agent — no second "system" actor is introduced.
    /// </summary>
    public int PriceCarBatch(int usedCarId, IReadOnlyList<PendingPricingPartDto> parts)
    {
        if (parts.Count == 0) return 0;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var car = session.Connection.QuerySingleOrDefault<CarCostRow>(
            "SELECT GrandTotalBase, ExpectedSellThroughRate, CreatedByUserId FROM dbo.UsedCars WHERE Id = @UsedCarId;",
            new { UsedCarId = usedCarId },
            session.Transaction);

        if (car is null) return 0;

        var userId = car.CreatedByUserId ?? 0;

        var weights = parts.Select(p => GetHeuristicWeight(p.Name)).ToList();
        var totalWeight = weights.Sum();
        var estimatedRetailPool = car.GrandTotalBase * EstimatedRetailValueMultiplier;

        var inputs = new List<UsedVehiclePartPricingInput>(parts.Count);
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            var estimatedMarketPrice = ResolveEstimatedMarketPrice(part.Name, weights[i], totalWeight, estimatedRetailPool);
            inputs.Add(new UsedVehiclePartPricingInput(part.PartId, estimatedMarketPrice, null, 0m));
        }

        var result = UsedVehiclePartPricingEngine.Calculate(car.GrandTotalBase, car.ExpectedSellThroughRate, inputs);

        var pricedCount = 0;
        var now = DateTime.UtcNow;

        foreach (var row in result.Rows)
        {
            if (row.RecommendedPrice <= 0m)
            {
                // Not enough information yet (e.g. the car's own cost hasn't been recorded) —
                // leave it as "Manual" so a human can look at it, and so the agent retries later.
                continue;
            }

            session.Connection.Execute(
                @"UPDATE dbo.Parts
                  SET EstimatedMarketPrice = @ExpectedMarketPrice,
                      CostAllocationPercent = @CostAllocationPercent,
                      AllocatedCost = @AllocatedCost,
                      CostPrice = @AllocatedCost,
                      MinimumSellPrice = @MinimumSellPrice,
                      FastSalePrice = @FastSalePrice,
                      WholesalePrice = @WholesalePrice,
                      RecommendedPrice = @RecommendedPrice,
                      SalePrice = @RecommendedPrice,
                      PricingStatus = @PricingStatus,
                      PricingCalculatedAt = @Now,
                      IsActive = 1,
                      ModifiedAt = @Now,
                      ModifiedByUserId = @UserId
                  WHERE Id = @PartId;",
                new
                {
                    row.ExpectedMarketPrice,
                    row.CostAllocationPercent,
                    row.AllocatedCost,
                    row.MinimumSellPrice,
                    row.FastSalePrice,
                    row.WholesalePrice,
                    row.RecommendedPrice,
                    PricingStatus = PartPricingStatus.Calculated,
                    Now = now,
                    UserId = userId,
                    PartId = row.PartId
                },
                session.Transaction);

            pricedCount++;
        }

        session.Commit();
        return pricedCount;
    }

    private decimal? ResolveEstimatedMarketPrice(string partName, decimal weight, decimal totalWeight, decimal estimatedRetailPool)
    {
        var index = _marketPriceIndex.GetIndex(partName);
        if (index is not null && index.SampleCount >= 2 && index.AveragePrice is > 0m)
        {
            return index.AveragePrice;
        }

        if (totalWeight <= 0m) return null;

        return Math.Round(estimatedRetailPool * (weight / totalWeight), 2, MidpointRounding.AwayFromZero);
    }

    private static decimal GetHeuristicWeight(string partName)
    {
        var suggestion = CarCrushService.FindSuggestionByName(partName);
        if (suggestion is null) return BaseWeight;

        return (suggestion.IsHighValue, suggestion.IsFastMoving) switch
        {
            (true, true) => HighValueFastMovingWeight,
            (true, false) => HighValueWeight,
            (false, true) => FastMovingWeight,
            _ => BaseWeight
        };
    }
}
