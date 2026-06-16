using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class CreatePartReservationRequest
    {
        public int PartId { get; set; }

        [Required]
        [MaxLength(120)]
        public string ReservedByName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? ReservedByPhone { get; set; }

        [Range(1, 999)]
        public int Quantity { get; set; } = 1;

        [Range(1, 72)]
        public int HoldHours { get; set; } = 24;

        public string? Notes { get; set; }
    }
}
