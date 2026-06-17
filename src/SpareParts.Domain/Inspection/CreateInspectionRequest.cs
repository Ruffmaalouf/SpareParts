namespace SpareParts.Domain.Inspection;

public class CreateInspectionRequest
{
    public int PartId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Notes { get; set; }
}
