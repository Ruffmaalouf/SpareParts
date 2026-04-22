namespace SpareParts.Domain.Accounting
{
    public sealed class UpdatePostingSettingItemRequest
    {
        public string SettingKey { get; set; } = string.Empty;
        public int? AccountId { get; set; }
    }
}
