using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    /// <summary>One tile of the Ignition dashboard KPI band (Report 04 §03 kpis block).</summary>
    public sealed class IgnitionKpiTileModel
    {
        public string Title { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
        public string? Delta { get; init; }
        public Brush DeltaBrush { get; init; } = Brushes.LightGray;
    }
}
