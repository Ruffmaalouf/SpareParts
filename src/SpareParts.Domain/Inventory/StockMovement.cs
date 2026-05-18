using SpareParts.Domain.Common;

namespace SpareParts.Domain.Inventory
{
    public class StockMovement : AuditableEntity
    {
        public int PartId { get; set; }
        public int WarehouseId { get; set; }
        public int Quantity { get; set; }
        public StockMovementType MovementType { get; set; }
        public string? ScanCode { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public decimal UnitCost { get; set; }
    }
}
