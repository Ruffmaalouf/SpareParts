namespace SpareParts.Domain.BusinessPartners
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public int? AccountId { get; set; }
        public string? AccountCode { get; set; }
        public string? AccountName { get; set; }

        public string AccountDisplay => !AccountId.HasValue
            ? "Pending"
            : string.IsNullOrWhiteSpace(AccountCode)
                ? (AccountName ?? string.Empty)
                : string.IsNullOrWhiteSpace(AccountName)
                    ? AccountCode
                    : $"{AccountCode} · {AccountName}";
    }
}
