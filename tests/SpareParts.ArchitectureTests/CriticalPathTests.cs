using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.OwnerCockpit;
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
        Assert.Equal(21.5m, totals.TaxTotal);
        Assert.Equal(261.5m, totals.TotalAmount);
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
    public void StockMovement_GetAvailableStock_ShouldExcludeReservedQuantity()
    {
        var repo = new FakeInventoryRepository();
        var service = new InventoryService();

        repo.StockRows.Add(new Stock
        {
            Id = 1,
            PartId = 10,
            WarehouseId = 3,
            Quantity = 5,
            ReservedQuantity = 2
        });

        var available = service.GetAvailableStock(repo, partId: 10, warehouseId: 3);

        Assert.Equal(3, available);
    }

    [Fact]
    public void ReservationClock_DefaultExpiry_ShouldUseTomorrowAtSixPmLocalTime()
    {
        var now = new DateTimeOffset(2026, 5, 20, 10, 30, 0, TimeSpan.FromHours(3));

        var expiresAt = PartReservationClock.DefaultExpiresAtUtc(now);

        Assert.Equal(new DateTime(2026, 5, 21, 15, 0, 0, DateTimeKind.Utc), expiresAt);
    }

    [Theory]
    [InlineData("autorelease", PartReservationExpirationAction.AutoRelease)]
    [InlineData("StaffReminder", PartReservationExpirationAction.StaffReminder)]
    [InlineData(null, PartReservationExpirationAction.AutoRelease)]
    public void ReservationClock_NormalizeAction_ShouldReturnKnownDeadlineAction(string? value, string expected)
    {
        Assert.Equal(expected, PartReservationExpirationAction.Normalize(value));
    }

    [Fact]
    public void StockMovement_AdjustStock_ShouldRejectNegativeQuantity()
    {
        var repo = new FakeInventoryRepository();
        var service = new InventoryService();

        var exception = Assert.Throws<ConflictException>(() =>
            service.AdjustStock(repo, partId: 10, warehouseId: 3, quantityChange: -1, StockMovementType.Sale, DomainReferenceType.Sale, 101, 20m, userId: 7));

        Assert.Equal("Cannot reduce stock below zero for part 10 in warehouse 3.", exception.Message);
        Assert.Empty(repo.StockRows);
        Assert.Empty(repo.Movements);
    }

    [Fact]
    public void StockMovement_Transfer_ShouldMoveQuantityBetweenWarehouses()
    {
        var repo = new FakeInventoryRepository();
        var service = new InventoryService();

        service.AdjustStock(repo, partId: 10, warehouseId: 1, quantityChange: 3, StockMovementType.Purchase, DomainReferenceType.Purchase, 100, 20m, userId: 7);
        service.AdjustStock(repo, partId: 10, warehouseId: 1, quantityChange: -2, StockMovementType.TransferOut, DomainReferenceType.Transfer, null, 20m, userId: 7);
        service.AdjustStock(repo, partId: 10, warehouseId: 2, quantityChange: 2, StockMovementType.TransferIn, DomainReferenceType.Transfer, null, 20m, userId: 7);

        Assert.Equal(1, repo.GetStock(10, 1)?.Quantity);
        Assert.Equal(2, repo.GetStock(10, 2)?.Quantity);
        Assert.Contains(repo.Movements, movement => movement.MovementType == StockMovementType.TransferOut);
        Assert.Contains(repo.Movements, movement => movement.MovementType == StockMovementType.TransferIn);
    }

    [Fact]
    public void JournalPosting_SaleAccountingStrategy_ShouldBalance()
    {
        var strategy = CreateSaleAccountingStrategy();
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
    public void OwnerCockpit_DailyProfitLoss_ShouldSubtractRentAndLaborFromGrossProfit()
    {
        var report = OwnerCockpitDailyProfitLossCalculator.Build(
            new DateTime(2026, 5, 28),
            "USD",
            grossSales: 1000m,
            grossProfit: 380m,
            new[]
            {
                new OwnerCockpitExpenseBreakdownRowDto { Category = "Rent", AccountCode = "6100", AccountName = "Shop rent", Amount = 75m, EntryCount = 1 },
                new OwnerCockpitExpenseBreakdownRowDto { Category = "Labor", AccountCode = "6200", AccountName = "Staff wages", Amount = 125m, EntryCount = 2 },
                new OwnerCockpitExpenseBreakdownRowDto { Category = "Other", AccountCode = "6300", AccountName = "Utilities", Amount = 30m, EntryCount = 1 }
            });

        Assert.Equal(620m, report.CostOfGoodsSold);
        Assert.Equal(75m, report.RentExpense);
        Assert.Equal(125m, report.LaborExpense);
        Assert.Equal(230m, report.TotalOperatingExpenses);
        Assert.Equal(150m, report.NetProfitLoss);
    }

    [Theory]
    [InlineData("6100", "Monthly rent", "May payment", "Rent")]
    [InlineData("6200", "Operating Expenses", "Payroll and staff wages", "Labor")]
    [InlineData("6300", "Utilities", "Generator fuel", "Other")]
    public void OwnerCockpit_DailyProfitLoss_ShouldClassifyOperatingExpenses(string code, string account, string description, string expected)
    {
        var category = OwnerCockpitDailyProfitLossCalculator.ClassifyExpense(code, account, description);

        Assert.Equal(expected, category);
    }

    [Fact]
    public void ErrorHandling_PaymentPolicy_ShouldReturnPartialWhenPaidLessThanTotal()
    {
        var policy = new DefaultPaymentStatusPolicy();
        var status = policy.Resolve(totalAmount: 100m, paidAmount: 40m);

        Assert.Equal(PaymentStatus.PartiallyPaid, status);
    }

    [Fact]
    public void FailurePath_SaleAccountingStrategy_ShouldThrowForNegativeTotals()
    {
        var strategy = CreateSaleAccountingStrategy();
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
        var seededQuantity = workerCount * iterationsPerWorker;

        service.AdjustStock(repo, partId: 77, warehouseId: 2, quantityChange: seededQuantity, StockMovementType.Purchase, DomainReferenceType.Purchase, 499, 7m, userId: 12);

        Parallel.For(0, workerCount, _ =>
        {
            for (var i = 0; i < iterationsPerWorker; i++)
            {
                service.AdjustStock(repo, partId: 77, warehouseId: 2, quantityChange: -1, StockMovementType.Sale, DomainReferenceType.Sale, 501, 7m, userId: 12);
                service.AdjustStock(repo, partId: 77, warehouseId: 2, quantityChange: 1, StockMovementType.Purchase, DomainReferenceType.Purchase, 500, 7m, userId: 12);
            }
        });

        var stock = repo.GetStock(77, 2);
        Assert.NotNull(stock);
        Assert.Equal(seededQuantity, stock!.Quantity);
        Assert.Equal(workerCount * iterationsPerWorker * 2 + 1, repo.MovementCount);
    }

    [Fact]
    public void Rollback_AdjustStock_ShouldCompensateQuantityWhenMovementInsertFails()
    {
        var repo = new ThrowOnMovementInsertInventoryRepository();
        var service = new InventoryService();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            service.AdjustStock(repo, partId: 10, warehouseId: 1, quantityChange: 5, StockMovementType.Purchase, DomainReferenceType.Purchase, 100, 3m, userId: 9));

        Assert.Equal("Simulated movement insert failure.", exception.Message);
        var stock = repo.GetStock(10, 1);
        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Quantity);
    }

    [Fact]
    public void Validation_SaleItems_ShouldRejectDiscountGreaterThanLineSubtotal()
    {
        var items = new List<SaleItemDto>
        {
            new() { PartId = 1, Quantity = 1, UnitPrice = 50m, DiscountAmount = 60m, TaxRate = 0m }
        };

        var exception = Assert.Throws<ValidationException>(() => InvoiceRequestValidator.ValidateSaleItems(items));

        Assert.Equal("Sale line 1 discount cannot exceed the line subtotal.", exception.Message);
    }

    [Fact]
    public void Validation_SaleItems_ShouldAggregateDuplicatePartQuantities()
    {
        var items = new List<SaleItemDto>
        {
            new() { PartId = 9, Quantity = 2, UnitPrice = 10m, DiscountAmount = 0m, TaxRate = 0m },
            new() { PartId = 9, Quantity = 3, UnitPrice = 10m, DiscountAmount = 0m, TaxRate = 0m },
            new() { PartId = 10, Quantity = 1, UnitPrice = 10m, DiscountAmount = 0m, TaxRate = 0m }
        };

        var quantities = InvoiceRequestValidator.AggregateSaleQuantities(items);

        Assert.Equal(5, quantities[9]);
        Assert.Equal(1, quantities[10]);
    }

    [Theory]
    [InlineData(" spareparts://part/P-100 ", "part:P-100")]
    [InlineData("https://scanner.local/read?scan=warehouse%3AWH-2", "warehouse:WH-2")]
    [InlineData("PUR-20260430-000001", "PUR-20260430-000001")]
    public void Scanning_NormalizeScannedText_ShouldAcceptQrPayloads(string scannedText, string expected)
    {
        var normalized = ScanLookupService.NormalizeScannedText(scannedText);

        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void VisualSearch_ExtractSearchTokens_ShouldFavorPartHintsAndVisibleCodes()
    {
        var tokens = VisualPartSearchService.ExtractSearchTokens(
            "BMW brake disc sensor",
            "IMG_ATE-3434-front-rotor.jpg",
            "front brake rotor");

        Assert.Contains("bmw", tokens);
        Assert.Contains("brake", tokens);
        Assert.Contains("disc", tokens);
        Assert.Contains("ate", tokens);
        Assert.DoesNotContain("part", tokens);
    }

    private static SaleAccountingStrategy CreateSaleAccountingStrategy()
    {
        var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        var settingsProvider = new AccountingSettingsProvider(factory, new AccountingOptions
        {
            CashAccountId = 101,
            SalesAccountId = 401,
            CogsAccountId = 501,
            InventoryAccountId = 301,
            CashOrApAccountId = 999
        });

        var customerResolver = new CustomerAccountResolver(factory);
        return new SaleAccountingStrategy(factory, settingsProvider, customerResolver);
    }
}
