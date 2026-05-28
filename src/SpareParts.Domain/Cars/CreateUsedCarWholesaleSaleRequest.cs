namespace SpareParts.Domain.Cars
{
    public sealed class CreateUsedCarWholesaleSaleRequest
    {
        public int? CustomerId { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerPhone { get; set; }
        public DateTime SaleDate { get; set; } = DateTime.Today;
        public string CurrencyCode { get; set; } = "USD";
        public decimal SalePrice { get; set; }
        public decimal PaidAmount { get; set; }
        public bool IsForParts { get; set; }
        public List<UsedCarWholesaleRepairItemDto> RepairItems { get; set; } = new();
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public bool SoldAsIsAcknowledged { get; set; }
    }
}
