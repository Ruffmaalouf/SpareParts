using System;

namespace SpareParts.Domain.Inventory
{
    public sealed class PartRequestReservationDto
    {
        public int PartRequestId { get; set; }
        public int Quantity { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string ExpirationAction { get; set; } = PartReservationExpirationAction.AutoRelease;
    }
}
