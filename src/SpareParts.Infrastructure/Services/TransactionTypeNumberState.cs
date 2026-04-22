namespace SpareParts.Infrastructure.Services
{
    internal sealed class TransactionTypeNumberState
    {
        public string TypeKey { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string SerialNumberFormat { get; init; } = string.Empty;
        public long SerialCurrentNumber { get; init; }
    }
}
