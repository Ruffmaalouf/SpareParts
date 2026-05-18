using SpareParts.Domain.Common;

namespace SpareParts.Domain.Accounting
{
    public class JournalLine : AuditableEntity
    {
        public int JournalEntryId { get; set; }
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal OriginalAmount { get; set; }
        public decimal RateToBase { get; set; }
        public decimal CounterAmount { get; set; }
        public string BaseCurrencyCode { get; set; } = string.Empty;
        public string CounterCurrencyCode { get; set; } = string.Empty;
    }
}
