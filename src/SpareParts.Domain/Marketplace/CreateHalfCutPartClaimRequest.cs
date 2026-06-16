using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CreateHalfCutPartClaimRequest
    {
        public int HalfCutListingId { get; set; }

        [Required]
        [MaxLength(120)]
        public string ClaimantName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? ClaimantPhone { get; set; }

        [Required]
        [MaxLength(200)]
        public string PartName { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public decimal? DepositAmount { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }
}
