namespace SpareParts.Domain.Marketplace
{
    public class MarketPriceIndexDto
    {
        public string PartName { get; set; } = string.Empty;
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public decimal? AveragePrice { get; set; }
        public int SampleCount { get; set; }
        public string Currency { get; set; } = "USD";
        public string BasedOn { get; set; } = string.Empty;
        public DateTime ComputedAt { get; set; }
    }
}
