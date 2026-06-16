using SpareParts.Domain.Common;

namespace SpareParts.Domain.Sales
{
    public class SalesReturn : AuditableEntity
    {
        public string ReturnNumber { get; set; } = string.Empty;
        public int OriginalInvoiceId { get; set; }
        public DateTime ReturnDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalCost { get; set; }
        public decimal RefundAmount { get; set; }
        public string? Reason { get; set; }
    }
}
