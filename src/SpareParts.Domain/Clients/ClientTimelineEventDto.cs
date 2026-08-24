namespace SpareParts.Domain.Clients
{
    /// <summary>
    /// One event on the workspace Timeline tab — a SQL-side UNION ALL over the client's
    /// invoices, payments, quotes, and messages, ordered and paged in SQL (Report 04 §06/§08).
    /// </summary>
    public sealed class ClientTimelineEventDto
    {
        /// <summary>"invoice", "payment", "quote", or "message".</summary>
        public string EventType { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public decimal? Amount { get; set; }
        public string? Status { get; set; }
        public int? ReferenceId { get; set; }
    }
}
