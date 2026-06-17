namespace SpareParts.Domain.Ar;

public class ArScanResult
{
    public bool Success { get; set; }
    public List<ArMatchedPart> MatchedParts { get; set; } = new();
    public string? Confidence { get; set; }
    public string Disclaimer { get; set; } = "AR matching is in preview. Results may vary.";
}
