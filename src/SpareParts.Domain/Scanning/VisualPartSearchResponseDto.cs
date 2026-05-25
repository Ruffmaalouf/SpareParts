namespace SpareParts.Domain.Scanning;

public sealed class VisualPartSearchResponseDto
{
    public string Source { get; set; } = "metadata";
    public string SearchText { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> VisualTags { get; set; } = new();
    public List<VisualPartMatchDto> Matches { get; set; } = new();
}

public sealed class VisualPartMatchDto
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string? BrandName { get; set; }
    public string? CategoryName { get; set; }
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int AvailableQuantity { get; set; }
    public decimal Score { get; set; }
    public string MatchReason { get; set; } = string.Empty;
    public double AnchorX { get; set; }
    public double AnchorY { get; set; }
}
