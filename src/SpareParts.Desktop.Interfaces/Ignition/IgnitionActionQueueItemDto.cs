namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>One Action Queue entry of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionActionQueueItemDto
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>"receivables" | "inventory" | "messages" | ...</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>"danger" | "warning" | "info".</summary>
        public string Severity { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? CurrencyCode { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }
}
