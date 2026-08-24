using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    /// <summary>One row of the Ignition dashboard Action Queue (Report 04 §03 actionQueue block).</summary>
    public sealed class IgnitionActionQueueItemModel
    {
        public string Title { get; init; } = string.Empty;
        public string Detail { get; init; } = string.Empty;
        public string? AmountText { get; init; }

        /// <summary>"danger" | "warning" | "info" — raw severity from the contract.</summary>
        public string Severity { get; init; } = string.Empty;

        public Brush SeverityBrush { get; init; } = Brushes.LightGray;
        public string DeepLink { get; init; } = string.Empty;
    }
}
