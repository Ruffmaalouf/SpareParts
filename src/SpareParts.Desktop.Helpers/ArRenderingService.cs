using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public class ArRenderingService : IArRenderingService
    {
        public Task<ArOverlayFrame> RenderOverlayAsync(ArRenderRequest request, CancellationToken cancellationToken = default)
        {
            var normalizedCar = string.IsNullOrWhiteSpace(request.CarName) ? "Unknown car" : request.CarName.Trim();
            var normalizedPart = string.IsNullOrWhiteSpace(request.PartDescription) ? "Unknown part" : request.PartDescription.Trim();
            var vehicleHash = Math.Abs($"{normalizedCar}:{request.CarYear}:{request.EngineType}".GetHashCode());
            var partHash = Math.Abs($"{request.PartCode}:{normalizedPart}:{request.UnitPrice}".GetHashCode());

            var frame = new ArOverlayFrame
            {
                SessionId = Guid.NewGuid().ToString("N"),
                CarLabel = string.IsNullOrWhiteSpace(request.CarYear) ? normalizedCar : $"{normalizedCar} ({request.CarYear})",
                PartLabel = string.IsNullOrWhiteSpace(request.PartCode)
                    ? normalizedPart
                    : $"{request.PartCode} · {normalizedPart}",
                AnchorX = 0.15 + (vehicleHash % 55) / 100.0,
                AnchorY = 0.18 + (partHash % 52) / 100.0,
                Scale = 0.8 + ((vehicleHash + partHash) % 45) / 100.0,
                DiagnosticNote = $"Engine: {request.EngineType}; price: {request.UnitPrice:N2} USD"
            };

            return Task.FromResult(frame);
        }
    }
}
