namespace SpareParts.Domain.Sales
{
    public class CreateSaleResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "USD";
    }
}
