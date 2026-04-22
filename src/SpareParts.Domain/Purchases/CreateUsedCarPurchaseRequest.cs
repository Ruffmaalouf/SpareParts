using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Purchases
{
    public sealed class CreateUsedCarPurchaseRequest
    {
        [Range(1, int.MaxValue)]
        public int UsedCarId { get; set; }

        [Range(1, int.MaxValue)]
        public int SupplierId { get; set; }

        public DateTime PurchaseDate { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        [MaxLength(3)]
        public string BaseCurrencyCode { get; set; } = "USD";

        [Range(0, double.MaxValue)]
        public decimal PaidCounterAmount { get; set; }

        [MaxLength(3)]
        public string CounterCurrencyCode { get; set; } = "USD";

        [MaxLength(400)]
        public string Notes { get; set; } = string.Empty;

        [MinLength(1)]
        public List<CreateUsedCarPurchaseLineRequest> Lines { get; set; } = new();
    }
}
