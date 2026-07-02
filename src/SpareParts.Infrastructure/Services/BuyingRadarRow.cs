namespace SpareParts.Infrastructure.Services;

/// <summary>Raw part demand/supply projection used by <see cref="GrowthIntelligenceService"/> to build the buying radar list.</summary>
internal sealed class BuyingRadarRow
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal AvailableQuantity { get; set; }
    public decimal SoldQuantityLast90 { get; set; }
    public int WaitingCustomers { get; set; }
    public int MinStock { get; set; }
    public decimal SalePrice { get; set; }
}
