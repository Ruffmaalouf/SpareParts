using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    /// <summary>One aging bucket of the client workspace balance block (Report 04 §05).</summary>
    public sealed class IgnitionAgingBucketModel
    {
        public string Label { get; init; } = string.Empty;
        public string AmountText { get; init; } = string.Empty;

        /// <summary>Bar width in pixels, normalized against the largest bucket.</summary>
        public double BarWidth { get; init; }

        public Brush BarBrush { get; init; } = Brushes.Gray;
    }
}
