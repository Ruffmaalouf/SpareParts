using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Purchases
{
    public class CreatePurchaseRequest
    {
        public DateTime PurchaseDate { get; set; }
        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }
        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }
        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";
        [MinLength(1)]
        public List<PurchaseItemDto> Items { get; set; } = new();
    }
}
