namespace SpareParts.Domain.Purchases
{
    public sealed class UsedCarPurchaseSummaryDto
    {
        public int Id { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public int UsedCarId { get; set; }
        public string UsedCar { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string BaseCurrencyCode { get; set; } = "USD";
        public decimal TotalBaseAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public int LineCount { get; set; }
    }
}
