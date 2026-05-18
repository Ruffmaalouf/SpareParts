namespace SpareParts.Infrastructure.Services
{
    internal sealed class ExistingUsedCarState
    {
        public int Id { get; init; }
        public int? SupplierId { get; init; }
        public string PriceCurrency { get; init; } = "USD";
        public decimal Price { get; init; }
        public decimal PriceBase { get; init; }
        public decimal PriceCounter { get; init; }
        public int? LocationId { get; init; }
        public decimal Transportation { get; init; }
        public decimal PartOut { get; init; }
        public decimal Shipping { get; init; }
        public decimal Customs { get; init; }
        public decimal Repairs { get; init; }
        public decimal TotalBeforeShipping { get; init; }
        public decimal GrandTotalBase { get; init; }
        public decimal GrandTotalCounter { get; init; }
        public string BaseCurrencyCode { get; init; } = "USD";
        public string CounterCurrencyCode { get; init; } = "USD";
        public decimal CounterRateToBase { get; init; } = 1m;
        public bool IsReceived { get; init; }
        public DateTime? ReceivedAt { get; init; }
    }
}
