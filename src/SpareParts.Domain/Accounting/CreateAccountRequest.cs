namespace SpareParts.Domain.Accounting
{
    public sealed class CreateAccountRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccountTypeKey { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }
}
