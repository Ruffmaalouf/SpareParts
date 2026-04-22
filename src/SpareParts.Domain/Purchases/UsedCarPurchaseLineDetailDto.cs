namespace SpareParts.Domain.Purchases
{
    public sealed class UsedCarPurchaseLineDetailDto
    {
        public int Id { get; set; }
        public string DetailKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal RateToBase { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal CounterAmount { get; set; }
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
