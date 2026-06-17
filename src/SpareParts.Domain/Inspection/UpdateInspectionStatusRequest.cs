namespace SpareParts.Domain.Inspection;

public class UpdateInspectionStatusRequest
{
    public InspectionStatus NewStatus { get; set; }
    public string? Notes { get; set; }
    public string? VideoRoomUrl { get; set; }
}
