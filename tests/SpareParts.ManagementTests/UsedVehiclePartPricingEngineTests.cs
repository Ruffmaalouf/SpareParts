using SpareParts.Domain.Inventory;
using Xunit;

namespace SpareParts.ManagementTests;

public sealed class UsedVehiclePartPricingEngineTests
{
    [Fact]
    public void Calculate_allocates_vehicle_cost_by_expected_market_value()
    {
        var result = UsedVehiclePartPricingEngine.Calculate(
            vehicleTotalCost: 5750m,
            expectedSellThroughRate: 1m,
            parts:
            [
                new UsedVehiclePartPricingInput(1, 2500m, null, 0m),
                new UsedVehiclePartPricingInput(2, 1200m, null, 0m),
                new UsedVehiclePartPricingInput(3, 800m, null, 0m),
                new UsedVehiclePartPricingInput(4, 500m, null, 0m),
                new UsedVehiclePartPricingInput(5, 400m, null, 0m),
                new UsedVehiclePartPricingInput(6, 600m, null, 0m),
                new UsedVehiclePartPricingInput(7, 3000m, null, 0m)
            ]);

        var engine = Assert.Single(result.Rows, row => row.PartId == 1);

        Assert.Equal(9000m, result.TotalExpectedRevenue);
        Assert.Equal(27.7778m, engine.CostAllocationPercent);
        Assert.Equal(1597.22m, engine.AllocatedCost);
        Assert.Equal(engine.AllocatedCost, engine.MinimumSellPrice);
        Assert.Equal(1756.94m, engine.FastSalePrice);
        Assert.Equal(1916.66m, engine.WholesalePrice);
        Assert.Equal(2500m, engine.RecommendedPrice);
        Assert.Equal("Allocated", engine.PricingStatus);
    }

    [Fact]
    public void Calculate_inflates_cost_basis_for_unsold_inventory_factor()
    {
        var result = UsedVehiclePartPricingEngine.Calculate(
            vehicleTotalCost: 5750m,
            expectedSellThroughRate: 0.80m,
            parts:
            [
                new UsedVehiclePartPricingInput(1, 2500m, null, 0m),
                new UsedVehiclePartPricingInput(2, 6500m, null, 0m)
            ]);

        var engine = Assert.Single(result.Rows, row => row.PartId == 1);

        Assert.Equal(7187.50m, result.EffectiveVehicleCost);
        Assert.Equal(1996.53m, engine.MinimumSellPrice);
    }

    [Fact]
    public void Calculate_converts_allocated_base_cost_back_to_part_currency()
    {
        var usdToLbp = 89500m;
        var result = UsedVehiclePartPricingEngine.Calculate(
            vehicleTotalCost: 5750m * usdToLbp,
            expectedSellThroughRate: 1m,
            parts:
            [
                new UsedVehiclePartPricingInput(1, 2500m, null, 0m, usdToLbp),
                new UsedVehiclePartPricingInput(2, 6500m, null, 0m, usdToLbp)
            ]);

        var engine = Assert.Single(result.Rows, row => row.PartId == 1);

        Assert.Equal(27.7778m, engine.CostAllocationPercent);
        Assert.Equal(1597.22m, engine.AllocatedCost);
        Assert.Equal(1756.94m, engine.FastSalePrice);
        Assert.Equal(2500m, engine.RecommendedPrice);
    }

    [Fact]
    public void Calculate_uses_wholesale_floor_when_market_price_is_too_low()
    {
        var result = UsedVehiclePartPricingEngine.Calculate(
            vehicleTotalCost: 1000m,
            expectedSellThroughRate: 1m,
            parts: [new UsedVehiclePartPricingInput(1, 1000m, null, 0m)]);

        var row = Assert.Single(result.Rows);

        Assert.Equal(1000m, row.MinimumSellPrice);
        Assert.Equal(1200m, row.WholesalePrice);
        Assert.Equal(1200m, row.RecommendedPrice);
    }
}
