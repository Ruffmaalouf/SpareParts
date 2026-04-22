namespace SpareParts.Infrastructure.Services
{
    internal sealed class LocationLookup
    {
        public int LocationID { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal ShippingFees { get; init; }
        public string ShippingFeesCurrencyCode { get; init; } = "USD";
    }
}
