namespace SpareParts.Domain.Ar;

public class ArScanRequest
{
    public string? PhotoUrl { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
}
