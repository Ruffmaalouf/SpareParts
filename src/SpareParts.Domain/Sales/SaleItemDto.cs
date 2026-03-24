namespace SpareParts.Domain.Sales
{
    public class SaleItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
    }
}
