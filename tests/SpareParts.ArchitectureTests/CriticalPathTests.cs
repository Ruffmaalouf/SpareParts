using System.Data;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SpareParts.Api.Errors;
using SpareParts.ArchitectureTests.TestDoubles;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;
using SpareParts.Desktop.Wpf;

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

    [Fact]
    public void Transaction_DisposeWithoutCommit_ShouldRollback()
    {
        var tx = new FakeDbTransaction();
        var connection = new FakeDbConnection(tx);
        var factory = new FakeSqlConnectionFactory(connection);

        using (var session = new DbSession(factory))
        {
            // no-op; leaving scope should rollback
        }

        Assert.Equal(1, tx.RollbackCount);
        Assert.Equal(0, tx.CommitCount);
    }

    [Fact]
    public void DuplicateNumberContention_SalesGeneration_ShouldThrowConflictAfterRetries()
    {
        var handler = new CreateSaleHandler(
            factory: null!,
            inventoryService: null!,
            invoiceNumberGenerator: new FixedInvoiceNumberGenerator("INV-DUP-001", "PUR-UNUSED"),
            paymentStatusPolicy: null!,
            accountingStrategy: null!,
            totalsCalculator: null!);

        var salesRepository = new AlwaysDuplicateSalesRepository();

        var ex = Assert.Throws<TargetInvocationException>(() => InvokePrivate(handler, "GenerateUniqueSalesNumber", salesRepository));

        Assert.IsType<ConflictException>(ex.InnerException);
        Assert.Equal("Failed to generate a unique sales invoice number after multiple attempts.", ex.InnerException?.Message);
        Assert.Equal(5, salesRepository.CheckCount);
    }

    [Fact]
    public void DuplicateNumberContention_PurchaseGeneration_ShouldThrowConflictAfterRetries()
    {
        var handler = new CreatePurchaseHandler(
            factory: null!,
            inventoryService: null!,
            invoiceNumberGenerator: new FixedInvoiceNumberGenerator("INV-UNUSED", "PUR-DUP-001"),
            paymentStatusPolicy: null!,
            accountingStrategy: null!,
            totalsCalculator: null!);

        var purchasesRepository = new AlwaysDuplicatePurchasesRepository();

        var ex = Assert.Throws<TargetInvocationException>(() => InvokePrivate(handler, "GenerateUniquePurchaseNumber", purchasesRepository));

        Assert.IsType<ConflictException>(ex.InnerException);
        Assert.Equal("Failed to generate a unique purchase number after multiple attempts.", ex.InnerException?.Message);
        Assert.Equal(5, purchasesRepository.CheckCount);
    }

    [Fact]
    public async Task ApiToClientErrorEnvelope_Contract_ShouldRoundTripCodeMessageAndTraceId()
    {
        var middleware = new ApiExceptionMiddleware(
            _ => throw new ConflictException("Stock contention"),
            NullLogger<ApiExceptionMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-contract-123";
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context, new NoOpExceptionLogWriter());

        Assert.Equal((int)HttpStatusCode.Conflict, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();

        var serverEnvelope = JsonSerializer.Deserialize<SpareParts.Api.Errors.ApiErrorEnvelope>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(serverEnvelope);
        Assert.Equal("conflict", serverEnvelope!.Code);
        Assert.Equal("Stock contention", serverEnvelope.Message);
        Assert.Equal("trace-contract-123", serverEnvelope.TraceId);

        using var response = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var clientException = await Assert.ThrowsAsync<ApiClientException>(() => InvokeApiClientEnsureSuccessAsync(response, "fallback"));
        Assert.Equal(serverEnvelope.Code, clientException.Code);
        Assert.Equal(serverEnvelope.Message, clientException.Message);
        Assert.Equal(serverEnvelope.TraceId, clientException.TraceId);
    }

    private static void InvokePrivate(object instance, string methodName, object arg)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(instance, [arg]);
    }

    private static async Task InvokeApiClientEnsureSuccessAsync(HttpResponseMessage response, string fallback)
    {
        var method = typeof(ApiClientBase).GetMethod("EnsureSuccessAsync", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        var task = (Task)method!.Invoke(null, [response, fallback])!;
        await task;
    }

    private sealed class FakeSqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly IDbConnection _connection;

        public FakeSqlConnectionFactory(IDbConnection connection) => _connection = connection;

        public IDbConnection CreateConnection() => _connection;
    }

    private sealed class FakeDbConnection : IDbConnection
    {
        private readonly IDbTransaction _transaction;

        public FakeDbConnection(IDbTransaction transaction) => _transaction = transaction;

        public string ConnectionString { get; set; } = string.Empty;
        public int ConnectionTimeout => 0;
        public string Database => "test";
        public ConnectionState State => ConnectionState.Open;
        public IDbTransaction BeginTransaction() => _transaction;
        public IDbTransaction BeginTransaction(IsolationLevel il) => _transaction;
        public void ChangeDatabase(string databaseName) { }
        public void Close() { }
        public IDbCommand CreateCommand() => throw new NotSupportedException();
        public void Open() { }
        public void Dispose() { }
    }

    private sealed class FakeDbTransaction : IDbTransaction
    {
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public IDbConnection Connection => null!;
        public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;

        public void Commit() => CommitCount++;
        public void Rollback() => RollbackCount++;
        public void Dispose() { }
    }

    private sealed class FixedInvoiceNumberGenerator(string salesNumber, string purchaseNumber) : IInvoiceNumberGenerator
    {
        public string NextSalesNumber() => salesNumber;
        public string NextPurchaseNumber() => purchaseNumber;
    }

    private sealed class AlwaysDuplicateSalesRepository : ISalesRepository
    {
        public int CheckCount { get; private set; }

        public bool InvoiceNumberExists(string invoiceNumber)
        {
            CheckCount++;
            return true;
        }

        public int InsertInvoice(SalesInvoice invoice) => throw new NotSupportedException();
        public void InsertItems(int invoiceId, IList<SalesInvoiceItem> items) => throw new NotSupportedException();
        public List<SalesInvoiceLookupDto> SearchInvoices(string? query) => throw new NotSupportedException();
        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId) => throw new NotSupportedException();
    }

    private sealed class AlwaysDuplicatePurchasesRepository : IPurchasesRepository
    {
        public int CheckCount { get; private set; }

        public bool PurchaseNumberExists(string purchaseNumber)
        {
            CheckCount++;
            return true;
        }

        public int InsertInvoice(PurchaseInvoice invoice) => throw new NotSupportedException();
        public void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items) => throw new NotSupportedException();
    }

    private sealed class NoOpExceptionLogWriter : IExceptionLogWriter
    {
        public Task WriteAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
