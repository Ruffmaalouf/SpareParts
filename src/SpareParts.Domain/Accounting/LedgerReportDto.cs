using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class LedgerReportDto
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
        public List<LedgerRowDto> Entries { get; set; } = new();

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";
    }
}
