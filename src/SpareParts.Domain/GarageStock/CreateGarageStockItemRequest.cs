using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.GarageStock
{
    public class CreateGarageStockItemRequest
    {
        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(60)]
        public string? Barcode { get; set; }

        [MaxLength(60)]
        public string? OemNumber { get; set; }

        [Range(0, 999999)]
        public int Quantity { get; set; }

        [Range(0, 999999)]
        public int MinimumQuantity { get; set; }

        [MaxLength(60)]
        public string? PreferredSupplierId { get; set; }

        public string? Notes { get; set; }
    }
}
