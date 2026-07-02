namespace SpareParts.Infrastructure.Services;

/// <summary>Raw part candidate row used by <see cref="GrowthIntelligenceService"/> when scanning for duplicate/near-duplicate catalog parts.</summary>
internal sealed class DuplicateCandidateRow
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal SalePrice { get; set; }
}
