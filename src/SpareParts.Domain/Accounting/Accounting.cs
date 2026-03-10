using SpareParts.Domain.Common;

namespace SpareParts.Domain.Accounting
{
    public enum AccountType
    {
        Asset,
        Liability,
        Equity,
        Income,
        Expense
    }

    public class Account : AuditableEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public int? ParentId { get; set; }
    }

    public class JournalEntry : AuditableEntity
    {
        public DateTime EntryDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
    }

    public class JournalLine : AuditableEntity
    {
        public int JournalEntryId { get; set; }
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
