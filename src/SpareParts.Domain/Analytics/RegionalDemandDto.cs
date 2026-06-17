namespace SpareParts.Domain.Analytics;

public class RegionalDemandDto
{
    public string? Region { get; set; }
    public string? City { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int DemandCount { get; set; }
    public int SupplyCount { get; set; }
    public string InsightLabel { get; set; } = string.Empty;
}
