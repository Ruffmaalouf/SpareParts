namespace SpareParts.Domain.Accounting
{
    public sealed class TrialBalanceRowDto
    {
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountTypeKey { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
        public decimal TotalCounterDebit { get; set; }
        public decimal TotalCounterCredit { get; set; }
        public decimal CounterDebitBalance { get; set; }
        public decimal CounterCreditBalance { get; set; }

        public string AccountDisplay => string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";
    }
}
