using System.Collections.Generic;

namespace SpareParts.Domain.Auth
{
    public class UpdateRoleMenuAccessRequest
    {
        public List<RoleMenuAccessUpdateItem> Items { get; set; } = new();
    }

    public class RoleMenuAccessUpdateItem
    {
        public int MenuId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanModify { get; set; }
        public bool CanDelete { get; set; }
    }
}
