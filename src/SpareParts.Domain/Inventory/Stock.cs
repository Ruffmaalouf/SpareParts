using SpareParts.Domain.Common;

namespace SpareParts.Domain.Inventory
{
    public class Stock : AuditableEntity
    {
        public int PartId { get; set; }
        public int WarehouseId { get; set; }
        public int? LocationId { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
    }
}
