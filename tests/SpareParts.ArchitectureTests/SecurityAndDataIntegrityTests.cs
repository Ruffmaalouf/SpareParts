using System.Reflection;
using System.Net;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Controllers;
using SpareParts.Api.Hosting;
using SpareParts.Api.Services;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class SecurityAndDataIntegrityTests
{
    [Fact]
    public void ExcelImportController_ShouldRequireAdminPolicy()
    {
        var authorize = typeof(ExcelImportController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single(attribute => !string.IsNullOrWhiteSpace(attribute.Policy));

        Assert.Equal(AuthorizationPolicies.Admin, authorize.Policy);
    }

    [Fact]
    public void ExcelImportService_ShouldRejectSensitiveTablesAndAuditColumns()
    {
        Assert.True(ExcelImportService.IsTableAllowedForImport("dbo.Parts"));
        Assert.False(ExcelImportService.IsTableAllowedForImport("dbo.Users"));
        Assert.False(ExcelImportService.IsColumnAllowedForImport("dbo", "Parts", "CreatedByUserId"));
    }

    [Fact]
    public void ReportBuilderController_ShouldRequireAdminOrManagerPolicy()
    {
        var authorize = typeof(ReportBuilderController)
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single(attribute => !string.IsNullOrWhiteSpace(attribute.Policy));

        Assert.Equal(AuthorizationPolicies.AdminOrManager, authorize.Policy);
    }

    [Theory]
    [InlineData(nameof(ReportBuilderController.SaveLink))]
    [InlineData(nameof(ReportBuilderController.DeleteLink))]
    public void ReportBuilderLinkMutationEndpoints_ShouldRequireAdminPolicy(string methodName)
    {
        var method = typeof(ReportBuilderController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Single(attribute => !string.IsNullOrWhiteSpace(attribute.Policy));

        Assert.Equal(AuthorizationPolicies.Admin, authorize.Policy);
    }

    [Fact]
    public void ReportBuilderService_ShouldRejectSensitiveTables()
    {
        Assert.True(ReportBuilderService.IsTableAllowedForReporting("dbo.Parts"));
        Assert.False(ReportBuilderService.IsTableAllowedForReporting("dbo.Users"));
        Assert.False(ReportBuilderService.IsTableAllowedForReporting("dbo.ReportBuilderSavedReports"));
    }

    [Fact]
    public void ReportBuilderService_ShouldRejectManagerLinkMutationsBeforeOpeningConnection()
    {
        var service = new ReportBuilderService(null!);

        var exception = Assert.Throws<UnauthorizedAccessException>(() =>
            service.SaveLink(new SpareParts.Domain.Reports.SaveReportTableLinkRequest(), (int)UserRole.Manager));

        Assert.Equal("Only administrators can modify report table links.", exception.Message);
    }

    [Theory]
    [InlineData("CHANGE_ME_USE_ENV_OR_USER_SECRETS")]
    [InlineData("6533545btwrtrwrt4h563-placeholder-secret")]
    public void ApiComposition_ShouldRejectKnownJwtPlaceholderSecrets(string secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret
            })
            .Build();
        var resolveJwtSettings = typeof(SparePartsApiComposition)
            .GetMethod("ResolveJwtSettings", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(resolveJwtSettings);

        var exception = Assert.Throws<TargetInvocationException>(() =>
            resolveJwtSettings!.Invoke(null, [configuration]));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task AuthService_ExternalLogin_ShouldNotReactivateDisabledUser()
    {
        using var factory = new InMemorySqliteConnectionFactory();
        factory.InitializeSchema();
        using (var connection = factory.CreateConnection())
        {
            connection.Execute(
                @"INSERT INTO Users (Id, Username, FullName, Email, PasswordHash, RoleId, IsActive)
                  VALUES (11, 'google:disabled-user', 'Disabled User', 'disabled@example.com', 'unused', 4, 0);");
        }

        using var httpClient = new HttpClient(new StubHttpMessageHandler(
            """{"aud":"test-google-client","sub":"disabled-user","email":"disabled@example.com","email_verified":"true","name":"Disabled User"}"""));
        var service = new AuthService(
            factory,
            new JwtSettings
            {
                Secret = "test-jwt-secret-with-at-least-32-characters",
                Issuer = "tests",
                Audience = "tests"
            },
            new ExternalAuthSettings { GoogleClientId = "test-google-client" },
            httpClient);

        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ExternalLoginAsync(new ExternalLoginRequest
            {
                Provider = "google",
                IdToken = "test-token"
            }));

        Assert.Equal("This account is disabled.", exception.Message);
        using var verifyConnection = factory.CreateConnection();
        Assert.Equal(0, verifyConnection.ExecuteScalar<int>("SELECT IsActive FROM Users WHERE Id = 11;"));
    }

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
    public void AccountingMutationEndpoints_ShouldRequireAdminOrManagerRoleIdPolicy(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);

        var authorize = method!.GetCustomAttributes<AuthorizeAttribute>(inherit: true).FirstOrDefault();
        Assert.NotNull(authorize);
        Assert.Equal(AuthorizationPolicies.AdminOrManager, authorize!.Policy);
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
            RoleId = 2,
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

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
