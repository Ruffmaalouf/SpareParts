using System;

namespace SpareParts.Domain.Inventory
{
    public sealed class PartRequestDto
    {
        public int Id { get; set; }
        public int? PartId { get; set; }
        public string? PartInternalCode { get; set; }
        public string? MatchedPartName { get; set; }
        public int AvailableQuantity { get; set; }
        public int WaitingCustomerCount { get; set; } = 1;
        public bool IsReadyToContact { get; set; }
        public string SignalText => IsReadyToContact
            ? $"{Math.Max(1, WaitingCustomerCount)} waiting"
            : IsReservationReminderDue
                ? "Reminder due"
                : IsReserved
                    ? "Reserved"
                    : Status;
        public int ReservedQuantity { get; set; }
        public DateTime? ReservationExpiresAt { get; set; }
        public string? ReservationExpirationAction { get; set; }
        public DateTime? ReservationReminderSentAt { get; set; }
        public bool IsReserved { get; set; }
        public bool IsReservationOverdue { get; set; }
        public bool IsReservationReminderDue { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string RequestedPartName { get; set; } = string.Empty;
        public string? RequestedOemNumber { get; set; }
        public string? VehicleDetails { get; set; }
        public int Quantity { get; set; } = 1;
        public string Status { get; set; } = PartRequestStatus.Open;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}
