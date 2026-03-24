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
}
