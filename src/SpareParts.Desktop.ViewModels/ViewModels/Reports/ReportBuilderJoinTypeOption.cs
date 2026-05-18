namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderJoinTypeOption
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;

        public override string ToString() => DisplayName;
    }
}
