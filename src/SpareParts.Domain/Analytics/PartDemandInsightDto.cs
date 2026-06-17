namespace SpareParts.Domain.Analytics;

public class PartDemandInsightDto
{
    public string PartName { get; set; } = string.Empty;
    public int TotalDemand { get; set; }
    public int ActiveListings { get; set; }
    public int Gap { get; set; }
    public string InsightLabel { get; set; } = string.Empty;
}
