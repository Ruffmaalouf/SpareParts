namespace SpareParts.Infrastructure.Services;

/// <summary>Raw part pricing projection used by <see cref="UsedCarPartPricingAllocator"/> when reallocating a used car's cost across its parts.</summary>
internal sealed class UsedCarPricingPartRow
{
    public int PartId { get; set; }
    public decimal? EstimatedMarketPrice { get; set; }
    public decimal? AveragePrice { get; set; }
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
}
