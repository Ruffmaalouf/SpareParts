using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IArDeviceBridge
    {
        Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task PushOverlayFrameAsync(ArOverlayFrame payload, CancellationToken cancellationToken = default);
        bool IsConnected { get; }
        string LastConnectionDetails { get; }
        string? LastFramePath { get; }
    }
}
