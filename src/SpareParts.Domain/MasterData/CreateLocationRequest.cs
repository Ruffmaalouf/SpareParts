namespace SpareParts.Domain.MasterData
{
    public sealed class CreateLocationRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal ShippingFees { get; set; }
        public string ShippingFeesCurrencyCode { get; set; } = "USD";
    }
}
