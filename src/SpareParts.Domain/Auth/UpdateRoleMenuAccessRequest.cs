using System.Collections.Generic;

namespace SpareParts.Domain.Auth
{
    public class UpdateRoleMenuAccessRequest
    {
        public List<RoleMenuAccessUpdateItem> Items { get; set; } = new();
    }
}
