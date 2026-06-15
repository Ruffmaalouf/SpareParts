using Dapper;

namespace SpareParts.Infrastructure.Data
{
    internal sealed class CurrencyRateRow
    {
        public string Code { get; set; } = string.Empty;
        public decimal RateToUsd { get; set; }
        public string BaseCode { get; set; } = string.Empty;
    }
}
