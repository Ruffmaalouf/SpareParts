using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CreateEscrowRequest
    {
        public int? SalesInvoiceId { get; set; }

        public int? PartId { get; set; }

        [Required]
        [MaxLength(120)]
        public string BuyerName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? BuyerPhone { get; set; }

        [Required]
        [MaxLength(120)]
        public string SellerName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? SellerPhone { get; set; }

        [Required]
        [Range(0.01, 9999999)]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        public string? Notes { get; set; }

        public int? AutoReleaseAfterHours { get; set; }
    }
}
