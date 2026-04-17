namespace SpareParts.Domain.Accounting
{
    public sealed class PostingAccountSettingDto
    {
        public string SettingKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountTypeKey { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public bool IsConfigured { get; set; }

        public string AccountDisplay => !AccountId.HasValue
            ? "Not configured"
            : string.IsNullOrWhiteSpace(AccountCode)
            ? AccountName
            : $"{AccountCode} · {AccountName}";
    }
}
