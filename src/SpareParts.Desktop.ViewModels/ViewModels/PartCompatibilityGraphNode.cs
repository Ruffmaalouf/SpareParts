using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartCompatibilityGraphNode
    {
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public double X { get; init; }
        public double Y { get; init; }
        public double Radius { get; init; }
        public Brush Fill { get; init; } = Brushes.DimGray;
        public Brush Stroke { get; init; } = Brushes.LightGray;
        public Brush Foreground { get; init; } = Brushes.White;
        public double StrokeThickness { get; init; } = 2;
        public PartCompatibilityPartRow? Part { get; init; }
        public double Left => X - Radius;
        public double Top => Y - Radius;
        public double Diameter => Radius * 2;
    }
}
