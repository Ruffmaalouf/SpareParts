using SpareParts.Domain.Common;

namespace SpareParts.Domain.Accounting
{
    public class JournalEntry : AuditableEntity
    {
        public DateTime EntryDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
    }
}
