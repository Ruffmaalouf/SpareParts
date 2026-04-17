using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class LedgerRowDto
    {
        public int JournalEntryId { get; set; }
        public DateTime EntryDate { get; set; }
        public string? ReferenceType { get; set; }
        public int? ReferenceId { get; set; }
        public string? Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningBalance { get; set; }
    }

    public sealed class LedgerReportDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public List<LedgerRowDto> Entries { get; set; } = new();

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";
    }

    public sealed class TrialBalanceRowDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountTypeKey { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";
    }

    public sealed class TrialBalanceReportDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<TrialBalanceRowDto> Rows { get; set; } = new();
    }
}
