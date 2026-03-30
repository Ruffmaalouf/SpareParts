namespace SpareParts.Domain.MasterData
{
    public sealed class CurrencyRateDto
    {
        public string Code { get; set; } = string.Empty;
        public decimal RateToUsd { get; set; }
        public string BaseCode { get; set; } = string.Empty;
        public DateTime SnapshotUtc { get; set; }
    }
}
