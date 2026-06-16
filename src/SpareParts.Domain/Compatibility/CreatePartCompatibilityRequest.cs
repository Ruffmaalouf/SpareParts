using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Compatibility
{
    public class CreatePartCompatibilityRequest
    {
        public int PartId { get; set; }

        [MaxLength(500)]
        public string? CompatibleMakes { get; set; }

        [MaxLength(500)]
        public string? CompatibleModels { get; set; }

        [Range(1900, 2030)]
        public int? YearFrom { get; set; }

        [Range(1900, 2030)]
        public int? YearTo { get; set; }

        [MaxLength(200)]
        public string? CompatibleEngines { get; set; }

        [MaxLength(500)]
        public string? OemNumbers { get; set; }

        [MaxLength(500)]
        public string? AlternativeNumbers { get; set; }

        public string? Notes { get; set; }
    }
}
