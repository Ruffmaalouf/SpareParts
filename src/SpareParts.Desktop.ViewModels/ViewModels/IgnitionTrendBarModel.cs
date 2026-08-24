using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    /// <summary>One bar of the Ignition dashboard 7-day sales trend (Report 04 §03 salesTrend block).</summary>
    public sealed class IgnitionTrendBarModel
    {
        public string DateLabel { get; init; } = string.Empty;
        public string AmountText { get; init; } = string.Empty;

        /// <summary>Bar height in pixels, normalized against the series maximum.</summary>
        public double BarHeight { get; init; }

        public bool IsPeak { get; init; }
        public Brush BarBrush { get; init; } = Brushes.Gray;
    }
}
