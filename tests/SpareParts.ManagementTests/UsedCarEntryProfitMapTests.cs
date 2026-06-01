using SpareParts.Desktop.Wpf.Management;
using Xunit;

namespace SpareParts.ManagementTests;

public sealed class UsedCarEntryProfitMapTests
{
    [Fact]
    public void Unsold_remaining_stock_does_not_mark_break_even()
    {
        var sut = new UsedCarEntry
        {
            FullCostBase = 1000m,
            RemainingStockValueBase = 1500m,
            DisplayCurrencyCode = "USD",
            DisplayRateToBase = 1m
        };

        Assert.Equal(0m, sut.ProfitMapRecoveredValueBase);
        Assert.Equal(1000m, sut.ProfitMapBreakEvenGapBase);
        Assert.Equal(0m, sut.ProfitMapRecoveredPercent);
        Assert.Equal("USD 1,000.00 to break even", sut.ProfitMapBreakEvenStatus);
    }

    [Fact]
    public void Wholesale_sale_counts_as_recovered_value()
    {
        var sut = new UsedCarEntry
        {
            FullCostBase = 1000m,
            IsWholesaleSold = true,
            SalePriceBase = 1200m,
            RemainingStockValueBase = 1500m,
            DisplayCurrencyCode = "USD",
            DisplayRateToBase = 1m
        };

        Assert.Equal(1200m, sut.ProfitMapSoldValueBase);
        Assert.Equal(1200m, sut.ProfitMapRecoveredValueBase);
        Assert.Equal(0m, sut.ProfitMapBreakEvenGapBase);
        Assert.Equal("Break-even reached", sut.ProfitMapBreakEvenStatus);
    }
}
