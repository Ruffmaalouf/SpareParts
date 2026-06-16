using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.NeedBoard
{
    public class CreatePartWantedAdRequest
    {
        [Required]
        [MaxLength(120)]
        public string BuyerName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? BuyerPhone { get; set; }

        [Required]
        [MaxLength(200)]
        public string PartName { get; set; } = string.Empty;

        public string? Notes { get; set; }
        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? VinNumber { get; set; }
        public decimal? Budget { get; set; }
        public string? Currency { get; set; }
        public string? LocationCity { get; set; }
        public string? LocationCountry { get; set; }
        public string? Urgency { get; set; }
        public int? ExpiresInDays { get; set; } = 30;
    }
}
