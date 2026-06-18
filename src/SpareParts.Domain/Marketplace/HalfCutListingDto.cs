namespace SpareParts.Domain.Marketplace
{
    public class HalfCutListingDto
    {
        public int Id { get; set; }
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
        public string StatusLabel { get; set; } = string.Empty;
        public int ClaimsCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public string? SellerName { get; set; }
    }
}
