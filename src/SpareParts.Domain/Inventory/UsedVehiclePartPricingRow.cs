namespace SpareParts.Domain.Inventory
{
    public sealed record UsedVehiclePartPricingRow(
        int PartId,
        decimal ExpectedMarketPrice,
        decimal CostAllocationPercent,
        decimal AllocatedCost,
        decimal MinimumSellPrice,
        decimal FastSalePrice,
        decimal WholesalePrice,
        decimal RecommendedPrice,
        string PricingStatus);
}
