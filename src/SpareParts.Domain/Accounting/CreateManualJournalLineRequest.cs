namespace SpareParts.Domain.Accounting
{
    public sealed class CreateManualJournalLineRequest
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
