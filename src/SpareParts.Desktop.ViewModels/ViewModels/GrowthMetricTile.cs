using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class GrowthMetricTile
    {
        public GrowthMetricTile(string label, string value, string detail, Brush accentBrush)
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
