using System;
using System.Collections.Generic;
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
}
