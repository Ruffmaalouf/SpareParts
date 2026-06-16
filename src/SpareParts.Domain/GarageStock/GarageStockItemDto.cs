namespace SpareParts.Domain.GarageStock
{
    public class GarageStockItemDto
    {
        public int Id { get; set; }
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
        public bool IsLowStock { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
