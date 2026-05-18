using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class StatementOfAccountReportDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal OpeningCounterBalance { get; set; }
        public decimal ClosingCounterBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalCounterDebit { get; set; }
        public decimal TotalCounterCredit { get; set; }
        public string SubjectType { get; set; } = "Account";
        public int? SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public decimal PartnerOpeningBalance { get; set; }
        public decimal PartnerOpeningCounterBalance { get; set; }
        public decimal RemainingBalance { get; set; }
        public decimal RemainingCounterBalance { get; set; }
        public decimal TotalInvoiceAmount { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public decimal TotalJournalAmount { get; set; }
        public List<StatementOfAccountRowDto> Entries { get; set; } = new();
        public List<StatementCurrencyTotalDto> CurrencyTotals { get; set; } = new();

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";

        public string SubjectDisplay => string.IsNullOrWhiteSpace(SubjectName)
            ? AccountDisplay
            : SubjectName;
    }
}
