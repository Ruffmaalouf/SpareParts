using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public static class ThemeManager
    {
        // Map each theme to its ResourceDictionary URI (relative to project)
        private static readonly Dictionary<AppTheme, Uri> ThemeUris = new()
        {
            { AppTheme.MPower,        new Uri("Themes/BMWMTheme.xaml", UriKind.Relative) },
            { AppTheme.NeonGlow,      new Uri("Themes/NeonGlowTheme.xaml", UriKind.Relative) },
            { AppTheme.AMG,           new Uri("Themes/AMGTheme.xaml", UriKind.Relative) },
            { AppTheme.PorscheRS,     new Uri("Themes/PorscheRSTheme.xaml", UriKind.Relative) },
            { AppTheme.LamborghiniSC, new Uri("Themes/LamboTheme.xaml", UriKind.Relative) }
        };

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Default;

        public static void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            var dictionaries = app.Resources.MergedDictionaries;

            // Remove any previous theme dictionaries (non-default)
            var existingThemeDictionaries = dictionaries
                .Where(d => d.Source != null && ThemeUris.Values.Contains(d.Source))
                .ToList();

            foreach (var dict in existingThemeDictionaries)
                dictionaries.Remove(dict);

            // Default = just base theme stays (DefaultTheme.xaml)
            if (theme == AppTheme.Default)
            {
                CurrentTheme = AppTheme.Default;
                return;
            }

            // Add new theme dictionary
            if (ThemeUris.TryGetValue(theme, out var uri))
            {
                var newDict = new ResourceDictionary { Source = uri };
                dictionaries.Add(newDict);
                CurrentTheme = theme;
            }
        }
    }
    public enum AppTheme
    {
        Default,
        MPower,
        NeonGlow,
        AMG,
        PorscheRS,
        LamborghiniSC
    }
    public class ThemeOption
    {
        public AppTheme Key { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
