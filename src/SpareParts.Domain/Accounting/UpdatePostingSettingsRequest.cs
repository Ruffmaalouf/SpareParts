using System.Collections.Generic;

namespace SpareParts.Domain.Accounting
{
    public sealed class UpdatePostingSettingsRequest
    {
        public List<UpdatePostingSettingItemRequest> Items { get; set; } = new();
    }
}
