using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CreatePartReelRequest
    {
        public int? PartId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(500)]
        public string VideoUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        [MaxLength(200)]
        public string? PartName { get; set; }

        public decimal? Price { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }
}
