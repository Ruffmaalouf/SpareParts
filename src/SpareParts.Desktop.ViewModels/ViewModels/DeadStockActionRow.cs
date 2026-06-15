using SpareParts.Domain.Inventory;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class DeadStockActionRow
    {
        private DeadStockActionRow()
        {
        }

        public string Key { get; private init; } = string.Empty;
        public string Label { get; private init; } = string.Empty;
        public string Detail { get; private init; } = string.Empty;
        public string Tone { get; private init; } = "Neutral";
        public Brush ToneBrush { get; private init; } = Brushes.LightGray;

        public static DeadStockActionRow From(DeadStockActionDto source)
        {
            var tone = source.Tone ?? "Neutral";
            return new DeadStockActionRow
            {
                Key = source.Key ?? string.Empty,
                Label = source.Label ?? string.Empty,
                Detail = source.Detail ?? string.Empty,
                Tone = tone,
                ToneBrush = tone.Equals("Good", System.StringComparison.OrdinalIgnoreCase)
                    ? Accent("#FF81C784")
                    : tone.Equals("Warning", System.StringComparison.OrdinalIgnoreCase)
                        ? Accent("#FFFFB74D")
                        : Accent("#FF90A4AE")
            };
        }

        private static SolidColorBrush Accent(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }
    }
}
