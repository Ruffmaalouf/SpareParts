using System;
using System.Collections.Generic;
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
            var referenceImageUrls = BuildReferenceImageUrls(normalizedCar, request.CarYear, normalizedPart);
            var recommendedPartLabel = BuildRecommendedPartLabel(normalizedCar, request.CarYear, normalizedPart, request.PartCode);

            var frame = new ArOverlayFrame
            {
                SessionId = Guid.NewGuid().ToString("N"),
                CarLabel = string.IsNullOrWhiteSpace(request.CarYear) ? normalizedCar : $"{normalizedCar} ({request.CarYear})",
                PartLabel = recommendedPartLabel,
                AnchorX = 0.15 + (vehicleHash % 55) / 100.0,
                AnchorY = 0.18 + (partHash % 52) / 100.0,
                Scale = 0.8 + ((vehicleHash + partHash) % 45) / 100.0,
                DiagnosticNote = $"Engine: {request.EngineType}; price: {request.UnitPrice:N2} USD",
                ReferenceImageUrls = referenceImageUrls
            };

            return Task.FromResult(frame);
        }

        private static IReadOnlyList<string> BuildReferenceImageUrls(string normalizedCar, string carYear, string normalizedPart)
        {
            if (IsTargetE92M3(normalizedCar, carYear))
            {
                return new[]
                {
                    "https://commons.wikimedia.org/wiki/Special:FilePath/BMW_M3_E92_coupe_front.jpg",
                    "https://commons.wikimedia.org/wiki/Special:FilePath/BMW_S65_engine%2C_front_right.jpg",
                    "https://commons.wikimedia.org/wiki/Special:FilePath/BMW_S65_engine%2C_front.jpg"
                };
            }

            if (normalizedPart.Contains("engine", StringComparison.OrdinalIgnoreCase))
            {
                return new[] { "https://commons.wikimedia.org/wiki/Category:Automobile_engines" };
            }

            return Array.Empty<string>();
        }

        private static string BuildRecommendedPartLabel(string normalizedCar, string carYear, string normalizedPart, string partCode)
        {
            if (IsTargetE92M3(normalizedCar, carYear))
            {
                return "S65B40 · Complete Engine Assembly";
            }

            return string.IsNullOrWhiteSpace(partCode)
                ? normalizedPart
                : $"{partCode} · {normalizedPart}";
        }

        private static bool IsTargetE92M3(string normalizedCar, string carYear)
        {
            var isE92 = normalizedCar.Contains("E92", StringComparison.OrdinalIgnoreCase);
            var isM3 = normalizedCar.Contains("M3", StringComparison.OrdinalIgnoreCase);
            var isBmw = normalizedCar.Contains("BMW", StringComparison.OrdinalIgnoreCase);
            var is2010 = string.Equals(carYear, "2010", StringComparison.OrdinalIgnoreCase);

            return (isE92 || (isM3 && isBmw)) && is2010;
        }
    }
}
