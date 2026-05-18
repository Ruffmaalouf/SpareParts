namespace SpareParts.Domain.Accounting
{
    public sealed class StatementCurrencyTotalDto
    {
        public string Label { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
        public decimal OpeningBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
