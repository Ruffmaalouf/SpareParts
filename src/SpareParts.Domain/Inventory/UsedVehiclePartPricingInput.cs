namespace SpareParts.Domain.Inventory
{
    public sealed record UsedVehiclePartPricingInput(
        int PartId,
        decimal? EstimatedMarketPrice,
        decimal? AveragePrice,
        decimal SalePrice,
        decimal RateToBase = 1m);
}
