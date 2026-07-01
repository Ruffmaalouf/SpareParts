namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class CampaignOptionViewModel
    {
        public CampaignOptionViewModel(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public string Value { get; }
    }
}
