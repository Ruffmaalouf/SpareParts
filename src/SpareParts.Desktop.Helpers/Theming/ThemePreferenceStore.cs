using System;
using System.IO;
using System.Text.Json;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Persists the user's selected desktop theme to a small JSON file under
    /// %LocalAppData%\SpareParts\Preferences\theme.json, so the choice survives an app restart.
    /// Follows the same %LocalAppData%\SpareParts app-data convention already used for the
    /// startup crash log (see LoginViewModel.TryWriteStartupExceptionLog).
    /// </summary>
    public static class ThemePreferenceStore
    {
        private static readonly string PreferencesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SpareParts",
            "Preferences");

        private static readonly string ThemeFilePath = Path.Combine(PreferencesDirectory, "theme.json");

        public static void Save(AppTheme theme)
        {
            try
            {
                Directory.CreateDirectory(PreferencesDirectory);
                var json = JsonSerializer.Serialize(new ThemePreference { Theme = theme.ToString() });
                File.WriteAllText(ThemeFilePath, json);
            }
            catch
            {
                // Persisting the theme choice is a nice-to-have; never let it crash the app.
            }
        }

        public static AppTheme Load()
        {
            try
            {
                if (!File.Exists(ThemeFilePath))
                {
                    return AppTheme.Default;
                }

                var json = File.ReadAllText(ThemeFilePath);
                var preference = JsonSerializer.Deserialize<ThemePreference>(json);
                if (preference != null && Enum.TryParse<AppTheme>(preference.Theme, out var theme))
                {
                    return theme;
                }
            }
            catch
            {
                // Fall through to default if the file is missing/corrupt.
            }

            return AppTheme.Default;
        }
    }
}
