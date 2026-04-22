namespace SpareParts.Infrastructure.Data
{
    internal sealed class AccountingCurrencyContext
    {
        public string BaseCurrencyCode { get; init; } = "USD";
        public string CounterCurrencyCode { get; init; } = "USD";
        public decimal CounterRateToBase { get; init; } = 1m;
    }
}
