namespace SpareParts.Domain.Accounting
{
    public sealed class StatementPartyDto
    {
        public string PartyType { get; set; } = string.Empty;
        public int PartyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public int? AccountId { get; set; }
        public string? AccountCode { get; set; }
        public string? AccountName { get; set; }

        public string DisplayName => string.IsNullOrWhiteSpace(AccountCode)
            ? Name
            : $"{Name} ({AccountCode})";

        public string AccountDisplay => !AccountId.HasValue
            ? "Not linked"
            : string.IsNullOrWhiteSpace(AccountCode)
                ? (AccountName ?? string.Empty)
                : string.IsNullOrWhiteSpace(AccountName)
                    ? AccountCode
                    : $"{AccountCode} · {AccountName}";
    }
}
