using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class OwnerCockpitMetricCard
    {
        public string Title { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string Caption { get; init; } = string.Empty;
        public Brush AccentBrush { get; init; } = Brushes.DodgerBlue;
    }
}
