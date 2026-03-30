using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public class ArRenderingService : IArRenderingService
    {
        public Task<string> RenderOverlayAsync(string carName, string partName, CancellationToken cancellationToken = default)
        {
            var normalizedCar = string.IsNullOrWhiteSpace(carName) ? "Unknown car" : carName.Trim();
            var normalizedPart = string.IsNullOrWhiteSpace(partName) ? "Unknown part" : partName.Trim();
            var payload = $"overlay://car={Uri.EscapeDataString(normalizedCar)}&part={Uri.EscapeDataString(normalizedPart)}";
            return Task.FromResult(payload);
        }
    }
}
