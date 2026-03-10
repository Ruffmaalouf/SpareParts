using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class Location : AuditableEntity
    {
        public int WarehouseId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
