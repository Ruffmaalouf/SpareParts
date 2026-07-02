namespace SpareParts.Infrastructure.Services;

/// <summary>Raw used-car cost/currency lookup used by <see cref="UsedCarsService"/> when preparing a wholesale sale.</summary>
internal sealed class UsedCarWholesaleLookup
{
    public int Id { get; set; }
    public string? Barcode { get; set; }
    public string UsedCar { get; set; } = string.Empty;
    public int ModelYear { get; set; }
    public string BaseCurrencyCode { get; set; } = "USD";
    public string CounterCurrencyCode { get; set; } = "USD";
    public decimal CounterRateToBase { get; set; } = 1m;
    public decimal FullCostBase { get; set; }
}
