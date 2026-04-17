namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarDto
    {
        public int Id { get; set; }
        public int CarModelId { get; set; }
        public string Car { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public string PriceCurrency { get; set; } = "USD";
        public decimal Price { get; set; }
        public decimal PriceBase { get; set; }
        public decimal PriceCounter { get; set; }
        public int? LocationId { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Transportation { get; set; }
        public bool IsReceived { get; set; }
        public bool IsShipped { get; set; }
        public decimal PartOut { get; set; }
        public decimal Shipping { get; set; }
        public decimal Customs { get; set; }
        public decimal TotalBeforeShipping { get; set; }
        public decimal GrandTotalBase { get; set; }
        public decimal GrandTotalCounter { get; set; }
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal CounterRateToBase { get; set; } = 1m;
    }
}
