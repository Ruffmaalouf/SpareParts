using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IArRenderingService
    {
        Task<ArOverlayFrame> RenderOverlayAsync(ArRenderRequest request, CancellationToken cancellationToken = default);
    }
}
