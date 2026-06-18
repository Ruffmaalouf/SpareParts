namespace SpareParts.Domain.NeedBoard
{
    public class PartWantedAdDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? BuyerUserId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerPhone { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? VinNumber { get; set; }
        public decimal? Budget { get; set; }
        public string Currency { get; set; } = "USD";
        public string? LocationCity { get; set; }
        public string? LocationCountry { get; set; }
        public string Urgency { get; set; } = "Normal";
        public string? PhotoUrl { get; set; }
        public PartWantedAdStatus Status { get; set; } = PartWantedAdStatus.Open;
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public int OffersCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByUserId { get; set; }
    }
}
