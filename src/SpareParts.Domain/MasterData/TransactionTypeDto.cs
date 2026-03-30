namespace SpareParts.Domain.MasterData
{
    public sealed class TransactionTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
        public decimal CounterRate { get; set; } = 1m;
        public bool IsActive { get; set; } = true;
    }
}
