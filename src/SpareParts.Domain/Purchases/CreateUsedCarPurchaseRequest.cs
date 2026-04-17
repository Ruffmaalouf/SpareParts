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

        [MaxLength(400)]
        public string Notes { get; set; } = string.Empty;

        [MinLength(1)]
        public List<CreateUsedCarPurchaseLineRequest> Lines { get; set; } = new();
    }

    public sealed class CreateUsedCarPurchaseLineRequest
    {
        [MaxLength(80)]
        public string DetailKey { get; set; } = string.Empty;

        [MaxLength(160)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(3)]
        public string CurrencyCode { get; set; } = "USD";

        [Range(0, double.MaxValue)]
        public decimal RateToBase { get; set; }

        [Range(0, double.MaxValue)]
        public decimal BaseAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int AccountId { get; set; }

        public int SortOrder { get; set; }
    }
}
