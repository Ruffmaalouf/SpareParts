using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class UpdatePostingSettingsRequest
    {
        public List<UpdatePostingSettingItemRequest> Items { get; set; } = new();
    }

    public sealed class UpdatePostingSettingItemRequest
    {
        public string SettingKey { get; set; } = string.Empty;
        public int? AccountId { get; set; }
    }
}
