using SpareParts.Domain.Common;

namespace SpareParts.Domain.Purchases
{
    public sealed class UsedCarPurchase : AuditableEntity
    {
        public string PurchaseNumber { get; set; } = string.Empty;
        public int UsedCarId { get; set; }
        public int SupplierId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal TotalBaseAmount { get; set; }
        public decimal TotalCounterAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PaidCounterAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PostingStatus { get; set; } = "Draft";
        public DateTime? PostedAt { get; set; }
        public int? PostedByUserId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<UsedCarPurchaseLine> Lines { get; set; } = new();
    }

    public sealed class UsedCarPurchaseLine : AuditableEntity
    {
        public int UsedCarPurchaseId { get; set; }
        public string DetailKey { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal RateToBase { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal CounterAmount { get; set; }
        public int AccountId { get; set; }
        public int SortOrder { get; set; }
    }
}
