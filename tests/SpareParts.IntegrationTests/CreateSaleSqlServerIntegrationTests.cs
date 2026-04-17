using SpareParts.Domain.Common;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;

namespace SpareParts.IntegrationTests;

public class CreateSaleSqlServerIntegrationTests : IAsyncLifetime
{
    private readonly SqlServerSaleTestDatabase _database = new();

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task Handle_ShouldCreateInvoice_DecrementStock_AndWriteJournal()
    {
        if (!_database.IsAvailable)
        {
            return;
        }

        await _database.ResetSchemaAsync();
        await _database.SeedBaselineAsync(seedValidPostingAccounts: true, openingStock: 5);

        var handler = BuildHandler(_database.ConnectionString!);
        var request = BuildSaleRequest(quantity: 2);

        var response = handler.Handle(request, userId: 1);

        Assert.True(response.InvoiceId > 0);
        Assert.Equal(3, await _database.GetStockQuantityAsync(partId: 1, warehouseId: 1));
        Assert.Equal(1, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.SalesInvoices;"));
        Assert.Equal(1, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.SalesInvoiceItems;"));
        Assert.Equal(1, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.JournalEntries;"));
        Assert.Equal(4, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.JournalLines;"));
    }

    [Fact]
    public async Task Handle_ConcurrentSingleStock_ShouldAllowOnlyOneSale()
    {
        if (!_database.IsAvailable)
        {
            return;
        }

        await _database.ResetSchemaAsync();
        await _database.SeedBaselineAsync(seedValidPostingAccounts: true, openingStock: 1);

        var request = BuildSaleRequest(quantity: 1);
        var failures = 0;

        await Task.WhenAll(
            Task.Run(() =>
            {
                try
                {
                    BuildHandler(_database.ConnectionString!).Handle(request, userId: 1);
                }
                catch (ConflictException)
                {
                    Interlocked.Increment(ref failures);
                }
            }),
            Task.Run(() =>
            {
                try
                {
                    BuildHandler(_database.ConnectionString!).Handle(request, userId: 2);
                }
                catch (ConflictException)
                {
                    Interlocked.Increment(ref failures);
                }
            }));

        Assert.Equal(1, failures);
        Assert.Equal(0, await _database.GetStockQuantityAsync(partId: 1, warehouseId: 1));
        Assert.Equal(1, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.SalesInvoices;"));
    }

    [Fact]
    public async Task Handle_WhenJournalInsertFails_ShouldRollbackInvoiceAndStock()
    {
        if (!_database.IsAvailable)
        {
            return;
        }

        await _database.ResetSchemaAsync();
        await _database.SeedBaselineAsync(seedValidPostingAccounts: false, openingStock: 5);

        var handler = BuildHandler(_database.ConnectionString!);
        var request = BuildSaleRequest(quantity: 2);

        await Assert.ThrowsAnyAsync<Exception>(() => Task.Run(() => handler.Handle(request, userId: 1)));

        Assert.Equal(5, await _database.GetStockQuantityAsync(partId: 1, warehouseId: 1));
        Assert.Equal(0, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.SalesInvoices;"));
        Assert.Equal(0, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.StockMovements;"));
        Assert.Equal(0, await _database.ScalarIntAsync("SELECT COUNT(1) FROM dbo.JournalEntries;"));
    }

    private static CreateSaleRequest BuildSaleRequest(int quantity)
        => new()
        {
            InvoiceDate = DateTime.UtcNow,
            WarehouseId = 1,
            PaidAmount = 0,
            PaymentMethod = "Cash",
            Items =
            [
                new SaleItemDto
                {
                    PartId = 1,
                    Quantity = quantity,
                    UnitPrice = 20m,
                    DiscountAmount = 0m,
                    TaxRate = 0m
                }
            ]
        };

    private static CreateSaleHandler BuildHandler(string connectionString)
    {
        var factory = new SqlConnectionFactory(connectionString);
        var settingsProvider = new AccountingSettingsProvider(factory, new AccountingOptions());
        var strategy = new SaleAccountingStrategy(settingsProvider, new CustomerAccountResolver(factory));
        return new CreateSaleHandler(
            factory,
            new InventoryService(),
            new UtcInvoiceNumberGenerator(factory),
            new DefaultPaymentStatusPolicy(),
            strategy,
            new InvoiceTotalsCalculator());
    }
}
