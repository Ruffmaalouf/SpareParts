using System;
using System.Collections.Generic;

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

    public sealed class JournalEntryLineDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";
    }

    public sealed class JournalEntryDetailDto
    {
        public int Id { get; set; }
        public DateTime EntryDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public List<JournalEntryLineDto> Lines { get; set; } = new();
    }

    public sealed class CreateManualJournalEntryRequest
    {
        public DateTime EntryDate { get; set; } = DateTime.Today;
        public string Description { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = "Manual";
        public int? ReferenceId { get; set; }
        public List<CreateManualJournalLineRequest> Lines { get; set; } = new();
    }

    public sealed class CreateManualJournalLineRequest
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
