using System;
using System.ComponentModel;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public class ThemeOption : INotifyPropertyChanged
    {
        public AppTheme Key { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;

        private string _accentHex = "#FF5722";
        public string AccentHex
        {
            get => _accentHex;
            set
            {
                _accentHex = value;
                if (TryParseColor(value, out var accentColor))
                {
                    AccentColor = accentColor;
                    AccentBrush = new SolidColorBrush(AccentColor);
                }
                Notify(nameof(AccentHex));
                Notify(nameof(AccentColor));
                Notify(nameof(AccentBrush));
            }
        }

        public Color AccentColor { get; private set; } = Color.FromRgb(0xFF, 0x57, 0x22);
        public SolidColorBrush AccentBrush { get; private set; } = new(Color.FromRgb(0xFF, 0x57, 0x22));

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected == value) return; _isSelected = value; Notify(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        private static bool TryParseColor(string value, out Color color)
        {
            try
            {
                var converted = ColorConverter.ConvertFromString(value);
                if (converted is Color parsedColor)
                {
                    color = parsedColor;
                    return true;
                }
            }
            catch (FormatException)
            {
            }
            catch (NotSupportedException)
            {
            }

            color = default;
            return false;
        }
    }
}
