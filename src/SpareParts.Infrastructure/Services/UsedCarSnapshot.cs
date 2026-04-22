namespace SpareParts.Infrastructure.Services
{
    internal sealed class UsedCarSnapshot
    {
        public int SupplierId { get; init; }
        public int CarModelId { get; init; }
        public int ModelYear { get; init; }
        public string CarDisplayName { get; init; } = string.Empty;
        public string PriceCurrency { get; init; } = "USD";
        public decimal Price { get; init; }
        public decimal PriceBase { get; init; }
        public decimal PriceCounter { get; init; }
        public int LocationId { get; init; }
        public string Location { get; init; } = string.Empty;
        public decimal Transportation { get; init; }
        public bool IsReceived { get; init; }
        public bool IsShipped { get; init; }
        public decimal PartOut { get; init; }
        public decimal Shipping { get; init; }
        public decimal Customs { get; init; }
        public decimal TotalBeforeShipping { get; init; }
        public decimal GrandTotalBase { get; init; }
        public decimal GrandTotalCounter { get; init; }
        public string BaseCurrencyCode { get; init; } = "USD";
        public string CounterCurrencyCode { get; init; } = "USD";
        public decimal CounterRateToBase { get; init; } = 1m;
    }
}
