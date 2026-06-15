using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartCompatibilityGraphEdge
    {
        public PartCompatibilityGraphEdge(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Stroke = stroke;
            Thickness = thickness;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
        public Brush Stroke { get; }
        public double Thickness { get; }
    }
}
