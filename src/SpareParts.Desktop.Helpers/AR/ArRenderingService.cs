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
            var displayPrice = request.UnitPrice <= 0m
                ? EstimateMarketPrice(normalizedCar, request.CarYear, normalizedPart)
                : request.UnitPrice;
            var marketRange = BuildAiEstimatedRange(normalizedCar, normalizedPart, displayPrice);

            var frame = new ArOverlayFrame
            {
                SessionId = Guid.NewGuid().ToString("N"),
                CarLabel = string.IsNullOrWhiteSpace(request.CarYear) ? normalizedCar : $"{normalizedCar} ({request.CarYear})",
                PartLabel = recommendedPartLabel,
                AnchorX = 0.15 + (vehicleHash % 55) / 100.0,
                AnchorY = 0.18 + (partHash % 52) / 100.0,
                Scale = 0.8 + ((vehicleHash + partHash) % 45) / 100.0,
                DiagnosticNote = $"Engine: {request.EngineType}; list price: {displayPrice:N2} USD; AI est: {marketRange}",
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
                    "https://source.unsplash.com/1200x800/?bmw,m3,e92,engine",
                    "https://commons.wikimedia.org/wiki/Special:FilePath/BMW_M3_E92_coupe_front.jpg",
                    "https://commons.wikimedia.org/wiki/Special:FilePath/BMW_S65_engine%2C_front_right.jpg",
                    "https://commons.wikimedia.org/wiki/Special:FilePath/BMW_S65_engine%2C_front.jpg"
                };
            }

            if (normalizedPart.Contains("engine", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    $"https://source.unsplash.com/1200x800/?{Uri.EscapeDataString($"{normalizedCar} engine")}",
                    "https://source.unsplash.com/1200x800/?car,engine"
                };
            }

            var query = Uri.EscapeDataString($"{normalizedCar} {normalizedPart}");
            return new[]
            {
                $"https://source.unsplash.com/1200x800/?{query}",
                $"https://source.unsplash.com/1200x800/?{Uri.EscapeDataString(normalizedCar)}",
                $"https://source.unsplash.com/1200x800/?{Uri.EscapeDataString(normalizedPart)}"
            };
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

        private static decimal EstimateMarketPrice(string normalizedCar, string carYear, string normalizedPart)
        {
            if (IsTargetE92M3(normalizedCar, carYear) &&
                normalizedPart.Contains("engine", StringComparison.OrdinalIgnoreCase))
            {
                return 12500m;
            }

            if (normalizedPart.Contains("engine", StringComparison.OrdinalIgnoreCase))
            {
                return 6000m;
            }

            if (normalizedPart.Contains("filter", StringComparison.OrdinalIgnoreCase))
            {
                return 45m;
            }

            return 180m;
        }

        private static string BuildAiEstimatedRange(string normalizedCar, string normalizedPart, decimal centerPrice)
        {
            var lower = Math.Round(centerPrice * 0.82m, 2);
            var upper = Math.Round(centerPrice * 1.18m, 2);
            return $"${lower:N2} - ${upper:N2} USD ({normalizedCar} / {normalizedPart})";
        }
    }
}
