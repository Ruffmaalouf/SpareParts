namespace SpareParts.Domain.Sales
{
    public class SalesInvoiceLineDto
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
