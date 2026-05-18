using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public sealed class ArOverlayFrame
    {
        public string SessionId { get; set; } = string.Empty;
        public string CarLabel { get; set; } = string.Empty;
        public string PartLabel { get; set; } = string.Empty;
        public double AnchorX { get; set; }
        public double AnchorY { get; set; }
        public double Scale { get; set; }
        public string DiagnosticNote { get; set; } = string.Empty;
        public IReadOnlyList<string> ReferenceImageUrls { get; set; } = new List<string>();
    }
}
