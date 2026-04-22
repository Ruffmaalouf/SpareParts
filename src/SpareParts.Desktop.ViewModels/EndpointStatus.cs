namespace SpareParts.Desktop.Wpf
{
    internal sealed class EndpointStatus
    {
        public EndpointStatus(string label, string url)
        {
            Label = label;
            Url = url;
        }

        public string Label { get; }
        public string Url { get; }
        public bool IsOnline { get; set; }
    }
}
