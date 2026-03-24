using SpareParts.Domain.Common;

namespace SpareParts.Domain.Accounting
{
    public class JournalLine : AuditableEntity
    {
        public int JournalEntryId { get; set; }
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
