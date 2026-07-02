namespace SpareParts.Infrastructure.Services;

/// <summary>Raw part-under-donor-car projection used by <see cref="GrowthIntelligenceService"/> to build donor car part opportunities.</summary>
internal sealed class DonorPartRow
{
    public int UsedCarId { get; set; }
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int WaitingCustomers { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SuggestedPrice { get; set; }
}
