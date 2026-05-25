using System;

namespace SpareParts.Domain.Inventory
{
    public sealed class PartReservationReminderDto
    {
        public int PartRequestId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RequestedPartName { get; set; } = string.Empty;
        public int ReservedQuantity { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
