namespace SpareParts.Domain.Reports;

public class PriceReportDto
{
    public string PartName { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? AvgPrice { get; set; }
    public int SampleCount { get; set; }
    public string TrendDirection { get; set; } = "Stable";
    public string Currency { get; set; } = "USD";
    public DateTime GeneratedAt { get; set; }
}
