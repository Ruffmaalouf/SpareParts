namespace SpareParts.Domain.Pricing;

public class PriceGeniusSuggestion
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public decimal? MarketAverage { get; set; }
    public decimal? SuggestedRegularPrice { get; set; }
    public decimal? SuggestedFastSalePrice { get; set; }
    public decimal? SuggestedMaxProfitPrice { get; set; }
    public decimal? SuggestedMinPrice { get; set; }
    public int SampleCount { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Reasoning { get; set; }
    public bool IsProFeature { get; set; }
    public bool IsLocked { get; set; }
}
