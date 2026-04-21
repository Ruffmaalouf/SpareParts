using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Purchases
{
    public sealed class UpdatePurchaseRequest
    {
        public DateTime PurchaseDate { get; set; }

        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        [MinLength(1)]
        public List<PurchaseItemDto> Items { get; set; } = new();
    }
}
