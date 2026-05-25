using System.Collections.Generic;

namespace SpareParts.Domain.Inventory
{
    public sealed class PartReservationClockRunResult
    {
        public int ReleasedReservationCount { get; set; }
        public IReadOnlyList<PartReservationReminderDto> Reminders { get; set; } = [];
    }
}
