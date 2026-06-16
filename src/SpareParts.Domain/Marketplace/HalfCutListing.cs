using SpareParts.Domain.Common;

namespace SpareParts.Domain.Marketplace
{
    public class HalfCutListing : AuditableEntity
    {
        public int TenantId { get; set; }
        public string VehicleMake { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public int VehicleYear { get; set; }
        public string? VinNumber { get; set; }
        public string? Color { get; set; }
        public string? OriginCountry { get; set; }
        public int? Mileage { get; set; }
        public string? Notes { get; set; }
        public string? PhotoUrls { get; set; }
        public decimal? AskingPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public HalfCutStatus Status { get; set; } = HalfCutStatus.Available;
        public int ClaimsCount { get; set; } = 0;
    }
}
