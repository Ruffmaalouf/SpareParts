using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.MechanicDesk
{
    public class CreateRepairOrderItemRequest
    {
        [Required]
        [MaxLength(200)]
        public string PartName { get; set; } = string.Empty;

        [MaxLength(60)]
        public string? OemNumber { get; set; }

        [Range(1, 999)]
        public int Quantity { get; set; } = 1;

        [MaxLength(20)]
        public string? ConditionPreference { get; set; }

        public string? Notes { get; set; }

        public decimal? MaxBudget { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }
}
