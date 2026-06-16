using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class VoiceSearchRequest
    {
        [Required]
        [MaxLength(500)]
        public string Transcript { get; set; } = string.Empty;

        public string? Language { get; set; } = "en";

        [MaxLength(100)]
        public string? HintVehicleMake { get; set; }

        [MaxLength(100)]
        public string? HintVehicleModel { get; set; }

        public int? HintVehicleYear { get; set; }
    }
}
