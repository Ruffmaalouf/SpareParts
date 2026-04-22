using System;

namespace SpareParts.Domain.Accounting
{
    public sealed class LedgerRowDto
    {
        public int JournalEntryId { get; set; }
        public DateTime EntryDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public string CurrencyCode { get; set; } = "USD";
        public decimal OriginalAmount { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal CounterDebit { get; set; }
        public decimal CounterCredit { get; set; }
        public decimal RunningBalance { get; set; }
        public decimal RunningCounterBalance { get; set; }
    }
}
