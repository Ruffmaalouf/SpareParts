namespace SpareParts.Domain.Transactions
{
    public sealed class TransactionTimelineStepDto
    {
        public int Sequence { get; set; }
        public string StageKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? OccurredAt { get; set; }
        public string Summary { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}
