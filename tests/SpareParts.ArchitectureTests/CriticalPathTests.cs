using SpareParts.Domain.Accounting;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.ArchitectureTests.TestDoubles;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class CriticalPathTests
{
    [Fact]
    public void InvoiceCreation_Totals_ShouldCalculateExpectedValues()
    {
        var calc = new InvoiceTotalsCalculator();
        var items = new List<SaleItemDto>
        {
            new() { PartId = 1, Quantity = 2, UnitPrice = 100, DiscountAmount = 10, TaxRate = 10 },
            new() { PartId = 2, Quantity = 1, UnitPrice = 50, DiscountAmount = 0, TaxRate = 5 }
        };

        var totals = calc.CalculateSales(items);

        Assert.Equal(250m, totals.Subtotal);
        Assert.Equal(10m, totals.DiscountTotal);
        Assert.Equal(24.5m, totals.TaxTotal);
        Assert.Equal(264.5m, totals.TotalAmount);
    }

    [Fact]
    public void StockMovement_AdjustStock_ShouldWriteStockAndMovement()
    {
        var repo = new FakeInventoryRepository();
        var service = new InventoryService();

        service.AdjustStock(repo, partId: 10, warehouseId: 3, quantityChange: 5, StockMovementType.Purchase, "Purchase", 101, 20m, userId: 7);

        Assert.Single(repo.StockRows);
        Assert.Single(repo.Movements);
        Assert.Equal(5, repo.StockRows.Single().Quantity);
        Assert.Equal(StockMovementType.Purchase, repo.Movements.Single().MovementType);
    }

    [Fact]
    public void JournalPosting_SaleAccountingStrategy_ShouldBalance()
    {
        var strategy = new SaleAccountingStrategy(cashAccountId: 101, salesAccountId: 401, cogsAccountId: 501, inventoryAccountId: 301);
        var invoice = new SalesInvoice
        {
            TotalAmount = 200,
            TotalCost = 120,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        var lines = strategy.BuildJournalLines(invoice, userId: 1);
        var debit = lines.Sum(x => x.Debit);
        var credit = lines.Sum(x => x.Credit);

        Assert.Equal(debit, credit);
        Assert.Equal(320m, debit);
    }

    [Fact]
    public void ErrorHandling_PaymentPolicy_ShouldReturnPartialWhenPaidLessThanTotal()
    {
        var policy = new DefaultPaymentStatusPolicy();
        var status = policy.Resolve(totalAmount: 100m, paidAmount: 40m);

        Assert.Equal("Partial", status);
    }

}
