using System;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Row of GET /api/clients/{id}/timeline (Report 04 §06) — the new ClientTimelineService's
    /// UNION over invoices/payments/messages/quotes, ordered by event date and paged in SQL.
    /// </summary>
    public sealed class IgnitionClientTimelineEventDto
    {
        /// <summary>"invoice" | "payment" | "message" | "quote" (matches the types query filter).</summary>
        public string Type { get; set; } = string.Empty;

        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Reference { get; set; }
    }
}
