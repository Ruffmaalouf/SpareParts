namespace SpareParts.Domain.Purchases
{
    public sealed class PostUsedCarPurchaseResponse
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string PostingStatus { get; set; } = "Posted";
    }
}
