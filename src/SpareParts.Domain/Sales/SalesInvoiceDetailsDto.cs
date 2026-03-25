namespace SpareParts.Domain.Sales
{
    public class SalesInvoiceDetailsDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public List<SalesInvoiceLineDto> Items { get; set; } = new();
    }

    public class SalesInvoiceLineDto
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
