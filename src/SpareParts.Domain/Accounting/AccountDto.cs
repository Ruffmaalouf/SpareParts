namespace SpareParts.Domain.Accounting
{
    public sealed class AccountDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccountTypeKey { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? ParentCode { get; set; }
        public string? ParentName { get; set; }

        public string ParentDisplay =>
            string.IsNullOrWhiteSpace(ParentCode)
                ? "Root"
                : string.IsNullOrWhiteSpace(ParentName)
                    ? ParentCode
                    : $"{ParentCode} · {ParentName}";

        public string DisplayName => $"{Code} · {Name}";
    }
}
