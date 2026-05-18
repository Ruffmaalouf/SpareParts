using SpareParts.Domain.Common;

namespace SpareParts.Domain.Sales
{
    public class SalesInvoice : AuditableEntity
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? ScanCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public bool IsReturn { get; set; }
        public int? ParentInvoiceId { get; set; }
        public decimal TotalCost { get; set; }
        public List<SalesInvoiceItem> Items { get; set; } = new();
    }
}
