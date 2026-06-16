using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.MechanicDesk
{
    public class CreateRepairOrderQuoteRequest
    {
        public int RepairOrderItemId { get; set; }

        [Required]
        [MaxLength(120)]
        public string SellerName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? SellerPhone { get; set; }

        public int? PartId { get; set; }

        [Required]
        [Range(0.01, 9999999)]
        public decimal OfferedPrice { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [Required]
        [MaxLength(60)]
        public string Condition { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public int? ExpiresInHours { get; set; }
    }
}
