namespace SpareParts.Domain.Ar;

public class ArMatchedPart
{
    public int? PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
}
