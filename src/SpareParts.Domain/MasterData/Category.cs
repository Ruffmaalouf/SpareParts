using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class Category : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }
}
