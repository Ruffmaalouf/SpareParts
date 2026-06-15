namespace SpareParts.Domain.Inventory
{
    public sealed record UsedVehiclePartPricingResult(
        decimal VehicleTotalCost,
        decimal EffectiveVehicleCost,
        decimal ExpectedSellThroughRate,
        decimal TotalExpectedRevenue,
        IReadOnlyList<UsedVehiclePartPricingRow> Rows);
}
