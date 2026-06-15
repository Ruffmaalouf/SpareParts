namespace SpareParts.Domain.Inventory
{
    internal sealed record PricingSeed(
        UsedVehiclePartPricingInput Input,
        decimal ExpectedMarketPrice,
        decimal RateToBase,
        decimal ExpectedMarketPriceBase);
}
