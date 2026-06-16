using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.VehicleProfile
{
    public class CreateUserVehicleRequest
    {
        public string? Nickname { get; set; }
        public string? VinNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string Make { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2030)]
        public int Year { get; set; }

        public string? Trim { get; set; }
        public string? EngineCode { get; set; }
        public string? FuelType { get; set; }
        public int? Mileage { get; set; }
        public bool SetAsDefault { get; set; } = false;
    }
}
