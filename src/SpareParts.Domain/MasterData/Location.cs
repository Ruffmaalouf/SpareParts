using SpareParts.Domain.Common;

namespace SpareParts.Domain.MasterData
{
    public class Location : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public decimal ShippingFees { get; set; }
        public string ShippingFeesCurrencyCode { get; set; } = "USD";
    }
}
