namespace SpareParts.Infrastructure.Services
{
    internal sealed class UsedCarJournalLinePlan
    {
        public string DetailKey { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int AccountId { get; init; }
        public decimal BaseAmount { get; set; }
        public string CurrencyCode { get; init; } = "USD";
        public decimal OriginalAmount { get; init; }
        public decimal RateToBase { get; init; } = 1m;
        public decimal CounterAmount { get; init; }
    }
}
