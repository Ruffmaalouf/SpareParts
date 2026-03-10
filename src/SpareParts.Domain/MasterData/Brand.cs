using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class Brand : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
