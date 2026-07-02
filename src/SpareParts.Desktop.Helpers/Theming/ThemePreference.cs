namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Serialization shape for the persisted theme preference file
    /// (%LocalAppData%\SpareParts\Preferences\theme.json). See <see cref="ThemePreferenceStore"/>.
    /// </summary>
    public class ThemePreference
    {
        public string Theme { get; set; } = nameof(AppTheme.Default);
    }
}
