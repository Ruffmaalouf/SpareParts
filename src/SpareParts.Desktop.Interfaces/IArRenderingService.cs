using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    public interface IArRenderingService
    {
        Task<ArOverlayFrame> RenderOverlayAsync(ArRenderRequest request, CancellationToken cancellationToken = default);
    }
}
