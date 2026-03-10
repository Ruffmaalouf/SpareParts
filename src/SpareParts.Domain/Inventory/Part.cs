using SpareParts.Domain.Common;

namespace SpareParts.Domain.Inventory
{
    public enum PartCondition
    {
        New = 1,
        Used = 2,
        Rebuilt = 3
    }

    public class Part : AuditableEntity
    {
        public string InternalCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public PartCondition Condition { get; set; }
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int MinStock { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
