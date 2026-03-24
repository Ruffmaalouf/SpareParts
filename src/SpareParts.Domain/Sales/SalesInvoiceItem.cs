using SpareParts.Domain.Common;

namespace SpareParts.Domain.Sales
{
    public class SalesInvoiceItem : AuditableEntity
    {
        public int InvoiceId { get; set; }
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
    }
}
