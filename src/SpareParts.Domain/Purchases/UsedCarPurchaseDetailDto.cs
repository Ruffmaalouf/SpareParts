using System;
using System.Collections.Generic;
using SpareParts.Domain.Transactions;

namespace SpareParts.Domain.Purchases
{
    public sealed class UsedCarPurchaseDetailDto
    {
        public int Id { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string? ScanCode { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int UsedCarId { get; set; }
        public string UsedCar { get; set; } = string.Empty;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal TotalBaseAmount { get; set; }
        public decimal TotalCounterAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PaidCounterAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PostingStatus { get; set; } = "Draft";
        public DateTime? PostedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public List<UsedCarPurchaseLineDetailDto> Lines { get; set; } = new();
        public List<TransactionTimelineStepDto> Timeline { get; set; } = new();
    }
}
