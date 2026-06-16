namespace SpareParts.Domain.Sales
{
    public class SalesReturnLookupDto
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int OriginalInvoiceId { get; set; }
        public string OriginalInvoiceNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int? CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
