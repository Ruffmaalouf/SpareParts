using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CarCrushRequest
    {
        [MaxLength(17)]
        public string? VinNumber { get; set; }

        [MaxLength(100)]
        public string? VehicleMake { get; set; }

        [MaxLength(100)]
        public string? VehicleModel { get; set; }

        public int? VehicleYear { get; set; }

        [MaxLength(100)]
        public string? Trim { get; set; }

        [MaxLength(60)]
        public string? EngineCode { get; set; }
    }
}
