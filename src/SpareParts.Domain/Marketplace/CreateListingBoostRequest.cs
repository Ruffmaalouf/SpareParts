using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CreateListingBoostRequest
    {
        public int PartId { get; set; }

        [Range(1, 90)]
        public int DurationDays { get; set; } = 7;

        public decimal? Fee { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(200)]
        public string? PaymentReference { get; set; }
    }
}
