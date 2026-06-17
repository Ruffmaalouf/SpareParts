namespace SpareParts.Domain.Inspection;

public class InspectionRequestDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PartId { get; set; }
    public string? PartName { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Notes { get; set; }
    public InspectionStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string? VideoProvider { get; set; }
    public string? VideoRoomUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
