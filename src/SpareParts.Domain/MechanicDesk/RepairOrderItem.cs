using SpareParts.Domain.Common;

namespace SpareParts.Domain.MechanicDesk
{
    public class RepairOrderItem : AuditableEntity
    {
        public int RepairOrderId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public int Quantity { get; set; } = 1;
        public string ConditionPreference { get; set; } = "Any";
        public string? Notes { get; set; }
        public decimal? MaxBudget { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
