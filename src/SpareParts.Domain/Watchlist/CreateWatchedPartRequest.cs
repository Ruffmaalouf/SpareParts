using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Watchlist
{
    public class CreateWatchedPartRequest
    {
        [MaxLength(200)]
        public string? PartName { get; set; }

        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? VinNumber { get; set; }
        public decimal? MaxBudget { get; set; }
        public string? Currency { get; set; }
        public string? Notes { get; set; }
    }
}
