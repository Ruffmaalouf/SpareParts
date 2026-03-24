namespace SpareParts.Domain.Purchases
{
    public class CreatePurchaseResponse
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
