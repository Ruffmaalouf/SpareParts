using System.ComponentModel.DataAnnotations;
using SpareParts.Domain.Forecast;

namespace SpareParts.Infrastructure.Services;

public sealed class DismantlerForecastService
{
    private readonly MarketPriceIndexService _marketPriceIndex;

    public DismantlerForecastService(MarketPriceIndexService marketPriceIndex)
    {
        _marketPriceIndex = marketPriceIndex;
    }

    public DismantlerForecastResult Forecast(DismantlerForecastRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VehicleMake))
            throw new ValidationException("Vehicle make is required.");
        if (request.PurchasePrice <= 0)
            throw new ValidationException("Purchase price must be positive.");

        var staticParts = GetStaticParts(request.VehicleMake);
        var forecastLines = new List<PartForecastLine>();
        var totalEstimated = 0m;

        foreach (var partName in staticParts)
        {
            var index = _marketPriceIndex.GetIndex(partName, request.VehicleMake, request.VehicleModel, request.VehicleYear);
            var price = index?.AveragePrice ?? GetDefaultPrice(partName);
            var line = new PartForecastLine
            {
                PartName = partName,
                Category = GetCategory(partName),
                EstimatedPrice = Math.Round(price, 2),
                IsHighValue = price >= 500m,
                IsFastMoving = IsFastMovingPart(partName),
                ConfidenceLevel = index != null ? "High" : "Low"
            };
            forecastLines.Add(line);
            totalEstimated += price;
        }

        var profit = totalEstimated - request.PurchasePrice;
        var roi = request.PurchasePrice > 0 ? (profit / request.PurchasePrice) * 100m : 0m;

        return new DismantlerForecastResult
        {
            VehicleDescription = $"{request.VehicleYear} {request.VehicleMake} {request.VehicleModel}".Trim(),
            PurchasePrice = request.PurchasePrice,
            EstimatedPartOutValue = Math.Round(totalEstimated, 2),
            EstimatedProfit = Math.Round(profit, 2),
            EstimatedRoiPercent = Math.Round(roi, 1),
            Currency = request.Currency,
            TopParts = forecastLines.OrderByDescending(p => p.EstimatedPrice).Take(15).ToList(),
            Disclaimer = "Estimated values are based on market data. Actual results may vary."
        };
    }

    private static List<string> GetStaticParts(string make)
    {
        return new List<string>
        {
            "Engine", "Transmission", "Front Bumper", "Rear Bumper", "Hood",
            "Driver Door", "Passenger Door", "Rear Left Door", "Rear Right Door",
            "Alternator", "Starter Motor", "Radiator", "AC Compressor",
            "Power Steering Pump", "Front Axle", "Rear Axle",
            "Catalytic Converter", "Headlights", "Tail Lights",
            "Dashboard", "Seats", "Airbags", "Fuel Pump", "Brake Calipers"
        };
    }

    private static decimal GetDefaultPrice(string partName) => partName switch
    {
        "Engine" => 1200m,
        "Transmission" => 800m,
        "Catalytic Converter" => 600m,
        "AC Compressor" => 350m,
        "Alternator" => 250m,
        "Radiator" => 180m,
        "Front Bumper" => 200m,
        "Rear Bumper" => 150m,
        "Hood" => 180m,
        "Airbags" => 400m,
        "Headlights" => 120m,
        _ => 80m
    };

    private static string GetCategory(string partName) => partName switch
    {
        "Engine" or "Transmission" or "Front Axle" or "Rear Axle" => "Drivetrain",
        "Alternator" or "Starter Motor" or "Fuel Pump" => "Electrical",
        "Radiator" or "AC Compressor" => "Cooling",
        "Front Bumper" or "Rear Bumper" or "Hood" or "Driver Door" or "Passenger Door"
            or "Rear Left Door" or "Rear Right Door" => "Body",
        "Headlights" or "Tail Lights" => "Lighting",
        "Dashboard" or "Seats" => "Interior",
        "Brake Calipers" or "Power Steering Pump" => "Chassis",
        _ => "Other"
    };

    private static bool IsFastMovingPart(string partName) =>
        partName is "Alternator" or "Starter Motor" or "Fuel Pump" or "Brake Calipers"
            or "Headlights" or "Tail Lights" or "Radiator";
}
