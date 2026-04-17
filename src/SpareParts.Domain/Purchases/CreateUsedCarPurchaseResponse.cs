namespace SpareParts.Domain.Purchases
{
    public sealed class CreateUsedCarPurchaseResponse
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public decimal TotalBaseAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
