using System;

namespace SpareParts.Domain.Inventory
{
    public sealed class ReservePartRequestRequest
    {
        public int? Quantity { get; set; }
        public int? WarehouseId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string ExpirationAction { get; set; } = PartReservationExpirationAction.AutoRelease;
    }
}
