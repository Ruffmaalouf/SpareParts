using SpareParts.Domain.Inventory;
using System.Globalization;

namespace SpareParts.Desktop.Wpf.Pricing;

public static class SmartPricingCoach
{
    public static SmartPricingCoachResult Evaluate(PartDto? part, int waitingCustomers = 0)
    {
        if (part is null)
        {
            return new SmartPricingCoachResult();
        }

        return Evaluate(
            part.CostPrice,
            part.SalePrice,
            part.AveragePrice,
            part.Currency,
            part.AvailableQuantity,
            part.MinStock,
            waitingCustomers);
    }

    public static SmartPricingCoachResult Evaluate(
        decimal costPrice,
        decimal salePrice,
        decimal? averagePrice,
        string? currency,
        int availableQuantity,
        int minStock,
        int waitingCustomers = 0)
    {
        var normalizedCurrency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        var average = averagePrice.GetValueOrDefault();
        var waitingCount = Math.Max(0, waitingCustomers);
        var marginRate = salePrice > 0m ? (salePrice - costPrice) / salePrice : 0m;
        var isRare = availableQuantity > 0 && availableQuantity <= Math.Max(1, minStock == 0 ? 1 : minStock);
        var suggested = SuggestedPrice(average, costPrice, salePrice, waitingCount, isRare);
        var suggestedText = suggested > 0m ? PriceLabel(suggested, normalizedCurrency) : string.Empty;
        var usuallyText = average > 0m ? PriceLabel(average, normalizedCurrency) : string.Empty;
        var currentText = salePrice > 0m ? PriceLabel(salePrice, normalizedCurrency) : string.Empty;

        if (isRare && waitingCount > 0 && suggested > salePrice)
        {
            return new SmartPricingCoachResult
            {
                Tone = "opportunity",
                Badge = $"{waitingCount} waiting",
                Message = $"Stock is rare and {waitingCount} customer{(waitingCount == 1 ? " is" : "s are")} waiting. Suggested price: {suggestedText}.",
                SuggestedPrice = suggested
            };
        }

        if (salePrice <= 0m)
        {
            return new SmartPricingCoachResult
            {
                Tone = "warning",
                Badge = "Set price",
                Message = average > 0m
                    ? $"This part usually sells at {usuallyText}. Add a sale price before quoting."
                    : "Add a sale price before quoting this part.",
                SuggestedPrice = suggested
            };
        }

        if (average > 0m && salePrice < average * 0.9m)
        {
            var guidance = costPrice <= 0m || marginRate < 0.22m
                ? "Margin is low."
                : $"Suggested price: {suggestedText}.";

            return new SmartPricingCoachResult
            {
                Tone = "warning",
                Badge = "Below usual",
                Message = $"This part usually sells at {usuallyText}. You priced {currentText}. {guidance}",
                SuggestedPrice = suggested
            };
        }

        if (costPrice > 0m && marginRate < 0.22m)
        {
            return new SmartPricingCoachResult
            {
                Tone = "warning",
                Badge = "Low margin",
                Message = $"You priced {currentText}. Margin is low. Suggested price: {suggestedText}.",
                SuggestedPrice = suggested
            };
        }

        if (waitingCount > 0)
        {
            return new SmartPricingCoachResult
            {
                Tone = "opportunity",
                Badge = $"{waitingCount} waiting",
                Message = $"{waitingCount} customer{(waitingCount == 1 ? " is" : "s are")} waiting for this part. Suggested price: {(string.IsNullOrEmpty(suggestedText) ? currentText : suggestedText)}.",
                SuggestedPrice = suggested
            };
        }

        if (average <= 0m)
        {
            return new SmartPricingCoachResult
            {
                Tone = "neutral",
                Badge = "Add average",
                Message = "Add an average market price to unlock stronger pricing guidance.",
                SuggestedPrice = suggested
            };
        }

        if (salePrice > average * 1.15m)
        {
            return new SmartPricingCoachResult
            {
                Tone = "good",
                Badge = "Strong",
                Message = $"Price is above the usual {usuallyText} market range; margin looks protected.",
                SuggestedPrice = suggested
            };
        }

        return new SmartPricingCoachResult
        {
            Tone = "good",
            Badge = "Price ok",
            Message = $"Price is aligned with the usual {usuallyText} market range.",
            SuggestedPrice = suggested
        };
    }

    public static IReadOnlyDictionary<int, int> WaitingCustomersByPart(IEnumerable<PartRequestDto> requests)
    {
        return requests
            .Where(request => request.PartId is int && PartRequestStatus.IsActive(request.Status))
            .GroupBy(request => request.PartId!.Value)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var distinctCustomers = group
                        .Select(CustomerKey)
                        .Where(key => !string.IsNullOrWhiteSpace(key) && key != "|")
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    var reported = group.Max(request => Math.Max(0, request.WaitingCustomerCount));
                    return Math.Max(distinctCustomers, reported);
                });
    }

    public static decimal? ParseAveragePrice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var current))
        {
            return current;
        }

        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            ? invariant
            : null;
    }

    private static string CustomerKey(PartRequestDto request)
    {
        if (request.CustomerId is int customerId)
        {
            return $"id:{customerId}";
        }

        return $"{request.CustomerName.Trim().ToUpperInvariant()}|{(request.CustomerPhone ?? string.Empty).Trim()}";
    }

    private static decimal SuggestedPrice(decimal averagePrice, decimal costPrice, decimal salePrice, int waitingCustomers, bool isRare)
    {
        var marginFloor = costPrice > 0m ? costPrice / 0.72m : 0m;
        var marketAnchor = averagePrice > 0m ? averagePrice : salePrice;
        var demandLift = isRare && waitingCustomers > 0
            ? Math.Min(1.3m, 1.12m + waitingCustomers * 0.045m)
            : 1m;
        var suggested = Math.Max(Math.Max(marketAnchor * demandLift, marginFloor), salePrice);
        return RoundToNearestFive(suggested);
    }

    private static decimal RoundToNearestFive(decimal value)
    {
        return value <= 0m
            ? 0m
            : Math.Max(5m, Math.Round(value / 5m, 0, MidpointRounding.AwayFromZero) * 5m);
    }

    private static string PriceLabel(decimal value, string currency)
    {
        var format = decimal.Truncate(value) == value ? "N0" : "N2";
        return string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase)
            ? $"${value.ToString(format, CultureInfo.CurrentCulture)}"
            : $"{currency} {value.ToString(format, CultureInfo.CurrentCulture)}";
    }
}
