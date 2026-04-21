namespace SpareParts.Domain.Purchases
{
    public sealed class PurchaseInvoiceLineDto
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
