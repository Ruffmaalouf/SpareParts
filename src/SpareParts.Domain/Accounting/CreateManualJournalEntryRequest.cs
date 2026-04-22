using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class CreateManualJournalEntryRequest
    {
        public DateTime EntryDate { get; set; } = DateTime.Today;
        public string Description { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = "Manual";
        public int? ReferenceId { get; set; }
        public List<CreateManualJournalLineRequest> Lines { get; set; } = new();
    }
}
