namespace SpareParts.Infrastructure.Services;

internal sealed class PriceReportRow
{
    public string PartName { get; set; } = string.Empty;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? AvgPrice { get; set; }
    public int SampleCount { get; set; }
    public string? Currency { get; set; }
}
