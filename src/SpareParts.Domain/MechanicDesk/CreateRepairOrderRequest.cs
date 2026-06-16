using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.MechanicDesk
{
    public class CreateRepairOrderRequest
    {
        [Required]
        [MaxLength(120)]
        public string CustomerName { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? CustomerPhone { get; set; }

        [MaxLength(100)]
        public string? VehicleMake { get; set; }

        [MaxLength(100)]
        public string? VehicleModel { get; set; }

        public int? VehicleYear { get; set; }

        [MaxLength(17)]
        public string? VinNumber { get; set; }

        public string? Notes { get; set; }

        [MaxLength(100)]
        public string? LocationCity { get; set; }

        public DateTime? DeadlineAt { get; set; }

        public List<CreateRepairOrderItemRequest> Items { get; set; } = new();
    }
}
