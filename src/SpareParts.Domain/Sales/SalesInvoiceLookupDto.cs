namespace SpareParts.Domain.Sales
{
    public class SalesInvoiceLookupDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? ScanCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string CounterCurrencyCode { get; set; } = string.Empty;
    }
}
