using SpareParts.Domain.Common;

namespace SpareParts.Domain.Marketplace
{
    public class PartReservation : AuditableEntity
    {
        public int TenantId { get; set; }
        public int PartId { get; set; }
        public int? ReservedByUserId { get; set; }
        public string ReservedByName { get; set; } = string.Empty;
        public string? ReservedByPhone { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal? HoldFee { get; set; }
        public string Currency { get; set; } = "USD";
        public PartReservationStatus Status { get; set; } = PartReservationStatus.Active;
        public DateTime ExpiresAt { get; set; }
        public string? Notes { get; set; }
    }
}
