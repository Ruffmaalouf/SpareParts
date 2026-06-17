namespace SpareParts.Domain.Inspection;

public class InspectionRequestEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PartId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public DateTime? RequestedAt { get; set; }
    public string? Notes { get; set; }
    public InspectionStatus Status { get; set; }
    public string? VideoProvider { get; set; }
    public string? VideoRoomUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
