using SpareParts.Domain.Accounting;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class AccountingAccountRow
    {
        public int Id { get; init; }
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public AccountType AccountType { get; init; }
        public int? ParentId { get; init; }
        public bool IsConfiguredPostingAccount { get; init; }
        public string PostingRoleLabel { get; init; } = "Reference";
        public string UsageSummary { get; init; } = string.Empty;

        public string ParentDisplay => ParentId is int parentId ? $"#{parentId}" : "Root";
    }
}
