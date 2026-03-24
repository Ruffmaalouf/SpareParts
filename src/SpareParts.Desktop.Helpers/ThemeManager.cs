using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public static class ThemeManager
    {
        private static readonly Dictionary<AppTheme, Uri> ThemeUris = new()
        {
            { AppTheme.MPower,        new Uri("Themes/BMWMTheme.xaml",      UriKind.Relative) },
            { AppTheme.NeonGlow,      new Uri("Themes/NeonGlowTheme.xaml",  UriKind.Relative) },
            { AppTheme.AMG,           new Uri("Themes/AMGTheme.xaml",       UriKind.Relative) },
            { AppTheme.PorscheRS,     new Uri("Themes/PorscheRSTheme.xaml", UriKind.Relative) },
            { AppTheme.LamborghiniSC, new Uri("Themes/LamboTheme.xaml",     UriKind.Relative) },
        };

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Default;

        private const string ThemeTag = "AppThemeOverride";

        public static void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null) return;
            var dicts = app.Resources.MergedDictionaries;
            // Remove by tag, not by URI — WPF resolves URIs to absolute pack:// so relative comparison fails
            foreach (var d in dicts.Where(d => d.Contains(ThemeTag)).ToList())
                dicts.Remove(d);
            if (theme != AppTheme.Default && ThemeUris.TryGetValue(theme, out var uri))
            {
                var nd = new ResourceDictionary { Source = uri };
                nd[ThemeTag] = true;
                dicts.Add(nd);
            }
            CurrentTheme = theme;
        }
    }

    public enum AppTheme { Default, MPower, NeonGlow, AMG, PorscheRS, LamborghiniSC }

    public class ThemeOption : INotifyPropertyChanged
    {
        public AppTheme Key      { get; set; }
        public string   Name     { get; set; } = string.Empty;
        public string   SubTitle { get; set; } = string.Empty;

        private string _accentHex = "#FF5722";
        public string AccentHex
        {
            get => _accentHex;
            set
            {
                _accentHex = value;
                try
                {
                    AccentColor = (Color)ColorConverter.ConvertFromString(value);
                    AccentBrush = new SolidColorBrush(AccentColor);
                }
                catch { }
                Notify(nameof(AccentHex)); Notify(nameof(AccentColor)); Notify(nameof(AccentBrush));
            }
        }

        public Color           AccentColor { get; private set; } = Color.FromRgb(0xFF, 0x57, 0x22);
        public SolidColorBrush AccentBrush { get; private set; } = new SolidColorBrush(Color.FromRgb(0xFF, 0x57, 0x22));

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected == value) return; _isSelected = value; Notify(nameof(IsSelected)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
