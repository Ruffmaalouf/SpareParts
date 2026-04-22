namespace SpareParts.Domain.Accounting
{
    public sealed class SavePostingRoleRequest
    {
        public string RoleKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
