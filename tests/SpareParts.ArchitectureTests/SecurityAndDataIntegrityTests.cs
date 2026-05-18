using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Api.Controllers;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class SecurityAndDataIntegrityTests
{
    [Theory]
    [InlineData(typeof(AccountsController), nameof(AccountsController.Create))]
    [InlineData(typeof(AccountsController), nameof(AccountsController.Update))]
    [InlineData(typeof(AccountsController), nameof(AccountsController.Delete))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.CreateAccountType))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.UpdateAccountType))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.DeleteAccountType))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.CreatePostingRole))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.UpdatePostingRole))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.DeletePostingRole))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.UpdatePostingSettings))]
    [InlineData(typeof(AccountingController), nameof(AccountingController.CreateManualJournal))]
    public void AccountingMutationEndpoints_ShouldRequireAdminOrManager(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true).FirstOrDefault();
        Assert.NotNull(authorize);
        Assert.Equal("Admin,Manager", authorize!.Roles);
    }

    [Fact]
    public void UsersService_Update_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        var service = new UsersService(factory);
        var request = new UpdateUserRequest
        {
            FullName = "Missing User",
            Email = "missing@example.com",
            Role = "Manager",
            IsActive = true
        };

        var exception = Assert.Throws<NotFoundException>(() => service.Update(999, request));
        Assert.Equal("User not found.", exception.Message);
    }

    [Fact]
    public void UsersService_Deactivate_ShouldThrowNotFound_WhenUserDoesNotExist()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        var service = new UsersService(factory);

        var exception = Assert.Throws<NotFoundException>(() => service.Deactivate(999));
        Assert.Equal("User not found.", exception.Message);
    }

    [Fact]
    public void InventoryService_AtomicDecrement_ShouldAllowOnlyOneWinner_WhenStockHitsZero()
    {
        var repository = new TestDoubles.ThreadSafeInventoryRepository();
        var service = new InventoryService();

        service.AdjustStock(repository, 12, 1, 1, StockMovementType.Purchase, DomainReferenceType.Purchase, 900, 3m, 1);

        var errors = 0;
        Parallel.For(0, 2, _ =>
        {
            try
            {
                service.AdjustStock(repository, 12, 1, -1, StockMovementType.Sale, DomainReferenceType.Sale, 901, 3m, 1);
            }
            catch (ConflictException)
            {
                Interlocked.Increment(ref errors);
            }
        });

        var stock = repository.GetStock(12, 1);
        Assert.NotNull(stock);
        Assert.Equal(0, stock!.Quantity);
        Assert.Equal(1, errors);
    }

    [Fact]
    public void SaleAccountingStrategy_ShouldTreatRoundedTotalsAsBalanced()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();

        var settingsProvider = new AccountingSettingsProvider(factory, new AccountingOptions
        {
            CashAccountId = 101,
            SalesAccountId = 401,
            CogsAccountId = 501,
            InventoryAccountId = 301,
            CashOrApAccountId = 999
        });
        var strategy = new SaleAccountingStrategy(factory, settingsProvider, new CustomerAccountResolver(factory));
        var invoice = new SalesInvoice
        {
            CustomerId = null,
            TotalAmount = 10.00004m,
            TotalCost = 10.00003m
        };

        var lines = strategy.BuildJournalLines(invoice, 7);

        var totalDebit = decimal.Round(lines.Sum(line => line.Debit), 4, MidpointRounding.AwayFromZero);
        var totalCredit = decimal.Round(lines.Sum(line => line.Credit), 4, MidpointRounding.AwayFromZero);
        Assert.Equal(totalDebit, totalCredit);
    }
}
