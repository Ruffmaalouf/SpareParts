using SpareParts.Domain.Inventory;

namespace SpareParts.Api.Notifications;

public sealed record PartReservationReminderNotification(
    int PartRequestId,
    string CustomerName,
    string RequestedPartName,
    int ReservedQuantity,
    DateTime ExpiresAt,
    DateTimeOffset CreatedAt)
{
    public static PartReservationReminderNotification From(PartReservationReminderDto reminder)
        => new(
            reminder.PartRequestId,
            reminder.CustomerName,
            reminder.RequestedPartName,
            reminder.ReservedQuantity,
            reminder.ExpiresAt,
            DateTimeOffset.UtcNow);
}
