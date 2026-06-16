using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.SymptomSearch
{
    public class SymptomSearchRequest
    {
        [Required]
        public string Symptoms { get; set; } = string.Empty;
        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? Language { get; set; } = "en";
    }
}
