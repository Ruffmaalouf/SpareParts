using SpareParts.Domain.Common;

namespace SpareParts.Domain.Purchases
{
    public class PurchaseInvoice : AuditableEntity
    {
        public string PurchaseNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public List<PurchaseInvoiceItem> Items { get; set; } = new();
    }
}
