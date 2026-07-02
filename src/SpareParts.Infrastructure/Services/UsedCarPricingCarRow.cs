namespace SpareParts.Infrastructure.Services;

/// <summary>Raw <c>dbo.UsedCars</c> cost/currency projection used by <see cref="UsedCarPartPricingAllocator"/> to reprice linked parts.</summary>
internal sealed class UsedCarPricingCarRow
{
    public int Id { get; set; }
    public decimal GrandTotalBase { get; set; }
    public decimal ExpectedSellThroughRate { get; set; }
    public string BaseCurrencyCode { get; set; } = "USD";
    public string CounterCurrencyCode { get; set; } = "USD";
    public decimal CounterRateToBase { get; set; } = 1m;
}
