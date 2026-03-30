namespace SpareParts.Desktop.Wpf.Interfaces
{
    public sealed class ArRenderRequest
    {
        public string CarName { get; set; } = string.Empty;
        public string CarYear { get; set; } = string.Empty;
        public string EngineType { get; set; } = string.Empty;
        public string PartCode { get; set; } = string.Empty;
        public string PartDescription { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}
