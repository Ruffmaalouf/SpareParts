namespace SpareParts.Domain.Clients
{
    /// <summary>One sale invoice row for the workspace Invoices tab (Report 04 §06).</summary>
    public sealed class ClientInvoiceDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public bool IsReturn { get; set; }
        public string CurrencyCode { get; set; } = "USD";
    }
}
