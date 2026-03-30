using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IArRenderingService
    {
        Task<string> RenderOverlayAsync(string carName, string partName, CancellationToken cancellationToken = default);
    }
}
