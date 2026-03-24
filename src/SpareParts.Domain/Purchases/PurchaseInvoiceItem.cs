using SpareParts.Domain.Common;

namespace SpareParts.Domain.Purchases
{
    public class PurchaseInvoiceItem : AuditableEntity
    {
        public int PurchaseId { get; set; }
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
    }
}
