using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    /// <summary>
    /// Shared "metric tile" display row used by the small KPI strips shown at the top of
    /// several feature screens (Dead Stock Resurrection, Growth Lab, Part Compatibility,
    /// Repair Prep Board, Stock Arrival Theater). Consolidated in Round 2 of the WPF audit
    /// from five structurally-identical per-feature duplicates
    /// (DeadStockMetricTile, GrowthMetricTile, PartCompatibilityMetricTile,
    /// RepairPrepMetricTile, StockArrivalMetricTile) into this single type.
    /// </summary>
    public sealed class MetricTile
    {
        public MetricTile(string label, string value, string detail, Brush accentBrush)
        {
            Label = label;
            Value = value;
            Detail = detail;
            AccentBrush = accentBrush;
        }

        public string Label { get; }
        public string Value { get; }
        public string Detail { get; }
        public Brush AccentBrush { get; }
    }
}
