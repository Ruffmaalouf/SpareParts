namespace SpareParts.Domain.Purchases
{
    public class CreatePurchaseRequest
    {
        public DateTime PurchaseDate { get; set; }
        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public List<PurchaseItemDto> Items { get; set; } = new();
    }
}
