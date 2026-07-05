namespace SpareParts.Domain.Dashboard
{
    /// <summary>
    /// One server-scored action-queue item (Ignition Reports 04 §03 / 05 §4).
    /// Ordered by severity × money at stake; deep-links to the entity-level action.
    /// </summary>
    public sealed class DashboardActionQueueItemDto
    {
        /// <summary>Stable rule key, e.g. "receivables-overdue", "inventory-risk".</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Signal family, e.g. "receivables", "inventory", "messages".</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>"danger", "warning", or "info".</summary>
        public string Severity { get; set; } = "info";

        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? CurrencyCode { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }
}
