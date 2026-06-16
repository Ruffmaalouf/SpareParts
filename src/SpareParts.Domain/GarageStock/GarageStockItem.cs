using SpareParts.Domain.Common;

namespace SpareParts.Domain.GarageStock
{
    public class GarageStockItem : AuditableEntity
    {
        public int TenantId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Barcode { get; set; }
        public string? OemNumber { get; set; }
        public int Quantity { get; set; }
        public int MinimumQuantity { get; set; } = 0;
        public string? PreferredSupplierId { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
