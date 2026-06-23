using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class OwnerCockpitStatItem
    {
        public string Label { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public Brush AccentBrush { get; init; } = Brushes.DodgerBlue;
    }
}
