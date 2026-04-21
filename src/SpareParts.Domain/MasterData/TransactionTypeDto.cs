namespace SpareParts.Domain.MasterData
{
    public sealed class TransactionTypeDto
    {
        public int Id { get; set; }
        public string TypeKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
        public decimal CounterRate { get; set; } = 1m;
        public string SerialNumberFormat { get; set; } = "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}";
        public long StartNumber { get; set; } = 1;
        public long CurrentNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
