namespace SpareParts.Infrastructure.Services;

internal sealed class PriceIndexRow
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public decimal? AveragePrice { get; set; }
    public int SampleCount { get; set; }
    public string? Currency { get; set; }
}
