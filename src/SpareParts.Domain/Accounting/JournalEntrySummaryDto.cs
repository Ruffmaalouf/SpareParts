using System;

namespace SpareParts.Domain.Accounting
{
    public sealed class JournalEntrySummaryDto
    {
        public int Id { get; set; }
        public DateTime EntryDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public int LineCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}
