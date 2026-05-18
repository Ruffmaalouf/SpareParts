using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Inventory
{
    public static class PartRequestStatus
    {
        public const string Open = "Open";
        public const string Contacted = "Contacted";
        public const string Fulfilled = "Fulfilled";
        public const string Cancelled = "Cancelled";

        public static readonly IReadOnlySet<string> ActiveStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Open,
            Contacted
        };

        public static readonly IReadOnlySet<string> AllStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Open,
            Contacted,
            Fulfilled,
            Cancelled
        };

        public static bool IsActive(string? status)
            => !string.IsNullOrWhiteSpace(status) && ActiveStatuses.Contains(status);

        public static string Normalize(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return Open;
            }

            var trimmed = status.Trim();
            foreach (var candidate in AllStatuses)
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
