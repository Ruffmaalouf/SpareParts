using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class TrialBalanceReportDto
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalCounterDebit { get; set; }
        public decimal TotalCounterCredit { get; set; }
        public List<TrialBalanceRowDto> Rows { get; set; } = new();
    }
}
