using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Inventory
{
    public static class PartReservationExpirationAction
    {
        public const string AutoRelease = "AutoRelease";
        public const string StaffReminder = "StaffReminder";

        public static readonly IReadOnlySet<string> AllActions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            AutoRelease,
            StaffReminder
        };

        public static string Normalize(string? action)
        {
            if (string.IsNullOrWhiteSpace(action))
            {
                return AutoRelease;
            }

            var trimmed = action.Trim();
            foreach (var candidate in AllActions)
            {
                if (string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return trimmed;
        }
    }
}
