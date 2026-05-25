using System;

namespace SpareParts.Domain.Inventory
{
    public static class PartReservationClock
    {
        public static DateTime DefaultExpiresAtUtc(DateTimeOffset now)
        {
            var localTomorrowAtSix = new DateTimeOffset(
                now.Year,
                now.Month,
                now.Day,
                18,
                0,
                0,
                now.Offset).AddDays(1);

            return localTomorrowAtSix.UtcDateTime;
        }

        public static DateTime ResolveExpiresAtUtc(DateTime? requestedExpiresAt, DateTimeOffset now)
        {
            if (requestedExpiresAt is null)
            {
                return DefaultExpiresAtUtc(now);
            }

            var expiresAt = requestedExpiresAt.Value;
            if (expiresAt.Kind == DateTimeKind.Utc)
            {
                return expiresAt;
            }

            if (expiresAt.Kind == DateTimeKind.Local)
            {
                return expiresAt.ToUniversalTime();
            }

            return DateTime.SpecifyKind(expiresAt, DateTimeKind.Local).ToUniversalTime();
        }
    }
}
