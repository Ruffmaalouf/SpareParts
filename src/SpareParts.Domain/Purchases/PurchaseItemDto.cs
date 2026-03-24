namespace SpareParts.Domain.Purchases
{
    public class PurchaseItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
    }
}
