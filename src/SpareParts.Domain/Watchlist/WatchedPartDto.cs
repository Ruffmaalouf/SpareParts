namespace SpareParts.Domain.Watchlist
{
    public class WatchedPartDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string? PartName { get; set; }
        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? VinNumber { get; set; }
        public decimal? MaxBudget { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastAlertedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MatchCount { get; set; } = 0;
        public DateTime? LastMatchAt { get; set; }
    }
}
