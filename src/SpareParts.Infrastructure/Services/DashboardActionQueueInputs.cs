namespace SpareParts.Infrastructure.Services
{
    /// <summary>
    /// The two dashboard signals that are not already present on the owner-cockpit
    /// aggregate (Report 04 §02 Task B): recent message failures and the low-stock panel.
    /// Gathered on an independent DbSession so it can run in parallel with the cockpit load.
    /// </summary>
    public sealed class DashboardActionQueueInputs
    {
        public int FailedMessageCount { get; set; }
        public Domain.Dashboard.DashboardLowStockPanelDto LowStockPanel { get; set; } = new();
    }
}
