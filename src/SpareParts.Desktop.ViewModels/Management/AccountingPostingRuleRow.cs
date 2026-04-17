namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class AccountingPostingRuleRow
    {
        public string Area { get; init; } = string.Empty;
        public string Trigger { get; init; } = string.Empty;
        public string DebitAccounts { get; init; } = string.Empty;
        public string CreditAccounts { get; init; } = string.Empty;
        public string Notes { get; init; } = string.Empty;
    }
}
