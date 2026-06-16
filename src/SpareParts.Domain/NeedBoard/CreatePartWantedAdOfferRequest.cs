using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.NeedBoard
{
    public class CreatePartWantedAdOfferRequest
    {
        public int WantedAdId { get; set; }

        [Required]
        [MaxLength(120)]
        public string SellerName { get; set; } = string.Empty;

        public string? SellerPhone { get; set; }
        public int? PartId { get; set; }
        public decimal? OfferedPrice { get; set; }
        public string? Currency { get; set; }
        public string? Condition { get; set; }
        public string? Notes { get; set; }
        public int? ExpiresInDays { get; set; } = 7;
    }
}
