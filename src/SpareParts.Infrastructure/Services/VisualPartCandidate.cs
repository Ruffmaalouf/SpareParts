namespace SpareParts.Infrastructure.Services;

/// <summary>Raw catalog part candidate row used by <see cref="VisualPartSearchService"/> when ranking matches for a photo search.</summary>
internal sealed class VisualPartCandidate
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }
    public string? Notes { get; set; }
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int AvailableQuantity { get; set; }
}
