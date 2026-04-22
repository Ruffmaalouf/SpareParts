namespace SpareParts.Domain.Accounting
{
    public sealed class SaveAccountTypeRequest
    {
        public string TypeKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
