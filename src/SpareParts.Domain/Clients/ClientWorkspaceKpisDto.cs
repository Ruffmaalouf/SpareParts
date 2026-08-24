namespace SpareParts.Domain.Clients
{
    /// <summary>Client workspace header KPIs (Report 04 §05).</summary>
    public sealed class ClientWorkspaceKpisDto
    {
        public decimal LifetimeRevenue { get; set; }
        public int LifetimeInvoiceCount { get; set; }
        public int? AverageDaysToPay { get; set; }
        public int OpenQuotesCount { get; set; }
        public int ActivePartRequestsCount { get; set; }
        public int? LoyaltyPoints { get; set; }

        /// <summary>
        /// Reserved for a future loyalty-tier model; the current codebase tracks points only,
        /// so this is always null (kept so the wire contract matches Report 04 §05).
        /// </summary>
        public string? LoyaltyTier { get; set; }
    }
}
