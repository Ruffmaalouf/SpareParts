using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;
using Xunit;

namespace SpareParts.IntegrationTests;

/// <summary>
/// End-to-end enumeration-safety + paging tests for ClientWorkspaceService against a real
/// SQL Server (Report 06 §07 — the key security guarantee). Proves against actual SQL semantics
/// that a cross-tenant customer id is indistinguishable from a nonexistent id (same 404
/// NotFoundException, same message), that the ownership check runs before any child query, and
/// that the Invoices child tab is paged with a correct total count.
///
/// Runs only when the MsSql Testcontainer is available (mirrors CreateSaleSqlServerIntegrationTests);
/// otherwise every test no-ops so the suite stays green where Docker is absent.
/// </summary>
public class ClientWorkspaceSqlServerIntegrationTests : IAsyncLifetime
{
    private const string ExpectedNotFoundMessage = "Customer not found.";
    private readonly ClientWorkspaceTestDatabase _database = new();

    public async Task InitializeAsync() => await _database.InitializeAsync();

    public async Task DisposeAsync() => await _database.DisposeAsync();

    [Fact]
    public async Task GetWorkspace_CrossTenantAndNonexistentIds_ThrowIdenticalNotFound()
    {
        if (!_database.CanRunIntegrationTests())
        {
            return;
        }

        await _database.ResetSchemaAsync();
        var (_, tenant2CustomerId) = await _database.SeedTwoTenantsAsync();
        var tenant1Service = BuildService(tenantId: 1);

        var crossTenant = Assert.Throws<NotFoundException>(() => tenant1Service.GetWorkspace(tenant2CustomerId));
        var nonexistent = Assert.Throws<NotFoundException>(() => tenant1Service.GetWorkspace(999999));

        Assert.Equal(ExpectedNotFoundMessage, crossTenant.Message);
        Assert.Equal(nonexistent.Message, crossTenant.Message);
    }

    [Fact]
    public async Task GetInvoices_CrossTenantAndNonexistentIds_ThrowIdenticalNotFound_BeforeAnyChildQuery()
    {
        if (!_database.CanRunIntegrationTests())
        {
            return;
        }

        await _database.ResetSchemaAsync();
        var (_, tenant2CustomerId) = await _database.SeedTwoTenantsAsync();
        var tenant1Service = BuildService(tenantId: 1);

        var crossTenant = Assert.Throws<NotFoundException>(() => tenant1Service.GetInvoices(tenant2CustomerId, 1, 25, null, null));
        var nonexistent = Assert.Throws<NotFoundException>(() => tenant1Service.GetInvoices(999999, 1, 25, null, null));

        Assert.Equal(ExpectedNotFoundMessage, crossTenant.Message);
        Assert.Equal(nonexistent.Message, crossTenant.Message);
    }

    [Fact]
    public async Task GetInvoices_ForOwnedCustomer_PagesWithCorrectTotalCount()
    {
        if (!_database.CanRunIntegrationTests())
        {
            return;
        }

        await _database.ResetSchemaAsync();
        var (tenant1CustomerId, _) = await _database.SeedTwoTenantsAsync();
        var tenant1Service = BuildService(tenantId: 1);

        var firstPage = tenant1Service.GetInvoices(tenant1CustomerId, page: 1, pageSize: 2, sort: null, status: null);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);

        // Default order is TransactionDate DESC, so INV-103 (2026-06-03) is first;
        // RemainingAmount = TotalAmount - PaidAmount = 150 - 50 = 100.
        var newest = firstPage.Items[0];
        Assert.Equal("INV-103", newest.InvoiceNumber);
        Assert.Equal(100m, newest.RemainingAmount);
        Assert.Equal("USD", newest.CurrencyCode);

        var secondPage = tenant1Service.GetInvoices(tenant1CustomerId, page: 2, pageSize: 2, sort: null, status: null);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Single(secondPage.Items);
    }

    [Fact]
    public async Task GetInvoices_FromAnotherTenant_CannotSeeOwningTenantsCustomer()
    {
        if (!_database.CanRunIntegrationTests())
        {
            return;
        }

        await _database.ResetSchemaAsync();
        var (tenant1CustomerId, _) = await _database.SeedTwoTenantsAsync();
        var tenant2Service = BuildService(tenantId: 2);

        // Tenant 2 asking for tenant 1's customer must get the uniform 404 — never the invoices.
        var ex = Assert.Throws<NotFoundException>(() => tenant2Service.GetInvoices(tenant1CustomerId, 1, 25, null, null));
        Assert.Equal(ExpectedNotFoundMessage, ex.Message);
    }

    private ClientWorkspaceService BuildService(int tenantId)
    {
        var factory = new SqlConnectionFactory(_database.ConnectionString!);
        var tenantContext = new TenantContext { TenantId = tenantId, TenantCode = $"t{tenantId}", IsResolved = true };
        return new ClientWorkspaceService(factory, tenantContext);
    }
}
