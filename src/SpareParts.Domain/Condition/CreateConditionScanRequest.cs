namespace SpareParts.Domain.Condition;

public class CreateConditionScanRequest
{
    public int PartId { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public string? Notes { get; set; }
}
