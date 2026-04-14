namespace SpareParts.Domain.MasterData
{
    public sealed class LocationDto
    {
        public int LocationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ShippingFees { get; set; }
        public string ShippingFeesCurrencyCode { get; set; } = "USD";
    }
}
