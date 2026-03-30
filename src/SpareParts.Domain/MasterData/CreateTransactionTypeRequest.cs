namespace SpareParts.Domain.MasterData
{
    public sealed class CreateTransactionTypeRequest
    {
        public string Name { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
        public decimal CounterRate { get; set; } = 1m;
        public bool IsActive { get; set; } = true;
    }
}
