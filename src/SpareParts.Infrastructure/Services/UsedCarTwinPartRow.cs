namespace SpareParts.Infrastructure.Services;

internal sealed class UsedCarTwinPartRow
{
    public int PartId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public int Condition { get; set; }
    public DateTime ListedAt { get; set; }
    public decimal RemainingQuantity { get; set; }
    public DateTime? SoldAt { get; set; }
    public decimal? SoldAmountBase { get; set; }
}
