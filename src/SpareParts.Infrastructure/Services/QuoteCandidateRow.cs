namespace SpareParts.Infrastructure.Services;

/// <summary>Raw catalog part projection used by <see cref="GrowthIntelligenceService"/> when scoring candidates for a voice quote.</summary>
internal sealed class QuoteCandidateRow
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public decimal SalePrice { get; set; }
    public decimal AvailableQuantity { get; set; }
}
