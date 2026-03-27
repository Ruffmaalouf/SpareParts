using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
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

        service.AdjustStock(repo, partId: 10, warehouseId: 3, quantityChange: 5, StockMovementType.Purchase, DomainReferenceType.Purchase, 101, 20m, userId: 7);

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

        Assert.Equal("PartiallyPaid", status);
    }

    [Fact]
    public void FailurePath_SaleAccountingStrategy_ShouldThrowForNegativeTotals()
    {
        var strategy = new SaleAccountingStrategy(cashAccountId: 101, salesAccountId: 401, cogsAccountId: 501, inventoryAccountId: 301);
        var invoice = new SalesInvoice
        {
            TotalAmount = -1m,
            TotalCost = 10m
        };

        var ex = Assert.Throws<InvalidOperationException>(() => strategy.BuildJournalLines(invoice, userId: 5));

        Assert.Equal("Sale journal lines cannot be generated from negative totals.", ex.Message);
    }

    [Fact]
    public void Concurrency_InventoryAdjustments_ShouldPreserveExpectedQuantityAndMovementCount()
    {
        var repo = new ThreadSafeInventoryRepository();
        var service = new InventoryService();
        const int workerCount = 40;
        const int iterationsPerWorker = 10;

        Parallel.For(0, workerCount, _ =>
        {
            for (var i = 0; i < iterationsPerWorker; i++)
            {
                service.AdjustStock(repo, partId: 77, warehouseId: 2, quantityChange: 1, StockMovementType.Purchase, "Purchase", 500, 7m, userId: 12);
                service.AdjustStock(repo, partId: 77, warehouseId: 2, quantityChange: -1, StockMovementType.Sale, "Sale", 501, 7m, userId: 12);
            }
        });

        var stock = repo.GetStock(77, 2);
        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Quantity);
        Assert.Equal(workerCount * iterationsPerWorker * 2, repo.MovementCount);
    }

    [Fact]
    public void Rollback_AdjustStock_ShouldCompensateQuantityWhenMovementInsertFails()
    {
        var repo = new ThrowOnMovementInsertInventoryRepository();
        var service = new InventoryService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.AdjustStock(repo, partId: 10, warehouseId: 1, quantityChange: 5, StockMovementType.Purchase, "Purchase", 100, 3m, userId: 9));

        Assert.Equal("Simulated movement insert failure.", exception.Message);
        var stock = repo.GetStock(10, 1);
        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Quantity);
    }
}
