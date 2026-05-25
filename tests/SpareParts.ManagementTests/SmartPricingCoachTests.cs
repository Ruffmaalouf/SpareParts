using SpareParts.Desktop.Wpf.Pricing;
using SpareParts.Domain.Inventory;
using Xunit;

namespace SpareParts.ManagementTests;

public sealed class SmartPricingCoachTests
{
    [Fact]
    public void Evaluate_flags_below_usual_price_with_low_margin()
    {
        var result = SmartPricingCoach.Evaluate(
            costPrice: 50m,
            salePrice: 60m,
            averagePrice: 85m,
            currency: "USD",
            availableQuantity: 6,
            minStock: 1);

        Assert.Equal("warning", result.Tone);
        Assert.Equal("Below usual", result.Badge);
        Assert.Equal("This part usually sells at $85. You priced $60. Margin is low.", result.Message);
    }

    [Fact]
    public void Evaluate_lifts_rare_stock_price_when_customers_are_waiting()
    {
        var part = new PartDto
        {
            Id = 7,
            CostPrice = 50m,
            SalePrice = 60m,
            AveragePrice = 85m,
            Currency = "USD",
            AvailableQuantity = 1,
            MinStock = 2
        };
        var requests = new[]
        {
            new PartRequestDto { PartId = 7, Status = PartRequestStatus.Open, WaitingCustomerCount = 4, CustomerName = "A" },
            new PartRequestDto { PartId = 7, Status = PartRequestStatus.Contacted, CustomerName = "B" },
            new PartRequestDto { PartId = 7, Status = PartRequestStatus.Open, CustomerName = "C" },
            new PartRequestDto { PartId = 7, Status = PartRequestStatus.Open, CustomerName = "D" }
        };

        var waitingByPart = SmartPricingCoach.WaitingCustomersByPart(requests);
        var result = SmartPricingCoach.Evaluate(part, waitingByPart[7]);

        Assert.Equal(4, waitingByPart[7]);
        Assert.Equal("opportunity", result.Tone);
        Assert.Equal("4 waiting", result.Badge);
        Assert.Equal("Stock is rare and 4 customers are waiting. Suggested price: $110.", result.Message);
        Assert.Equal(110m, result.SuggestedPrice);
    }
}
