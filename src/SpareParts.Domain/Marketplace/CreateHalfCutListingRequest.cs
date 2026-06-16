using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CreateHalfCutListingRequest
    {
        [Required]
        [MaxLength(100)]
        public string VehicleMake { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string VehicleModel { get; set; } = string.Empty;

        [Required]
        [Range(1900, 2030)]
        public int VehicleYear { get; set; }

        [MaxLength(17)]
        public string? VinNumber { get; set; }

        [MaxLength(60)]
        public string? Color { get; set; }

        [MaxLength(100)]
        public string? OriginCountry { get; set; }

        public int? Mileage { get; set; }

        public string? Notes { get; set; }

        [MaxLength(2000)]
        public string? PhotoUrls { get; set; }

        public decimal? AskingPrice { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }
}
