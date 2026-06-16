namespace SpareParts.Domain.Sales
{
    public class CreateSalesReturnResponse
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int OriginalInvoiceId { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
