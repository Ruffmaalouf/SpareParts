namespace SpareParts.Infrastructure.Data
{
    internal sealed class ReturnCurrencyRow
    {
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal CounterRateToBase { get; set; } = 1m;
    }
}
