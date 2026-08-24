using Dapper;
using Testcontainers.MsSql;

namespace SpareParts.IntegrationTests;

/// <summary>
/// Minimal SQL Server fixture for the Ignition Client Workspace enumeration-safety +
/// paging tests (Report 06 §07). It creates only the tables the tested code paths touch:
/// dbo.Customers (with TenantId — normally added at runtime by TenantIdMigration), plus
/// dbo.Transactions and dbo.TransactionTypes for the paged Invoices tab. It deliberately
/// seeds two tenants so a cross-tenant id can be proven indistinguishable from a missing id.
/// </summary>
internal sealed class ClientWorkspaceTestDatabase : IAsyncDisposable
{
    private MsSqlContainer? _container;

    public string? ConnectionString { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? AvailabilityReason { get; private set; }

    public bool CanRunIntegrationTests() => IsAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithCleanUp(true)
                .Build();
            await _container.StartAsync();
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            AvailabilityReason = ex.Message;
        }
    }

    public async Task ResetSchemaAsync()
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL DROP TABLE dbo.Transactions;
            IF OBJECT_ID('dbo.TransactionTypes', 'U') IS NOT NULL DROP TABLE dbo.TransactionTypes;
            IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;

            CREATE TABLE dbo.Customers
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                TenantId INT NOT NULL,
                Name NVARCHAR(200) NOT NULL,
                Phone NVARCHAR(50) NULL,
                Email NVARCHAR(200) NULL,
                Address NVARCHAR(400) NULL
            );

            CREATE TABLE dbo.TransactionTypes
            (
                Id INT NOT NULL PRIMARY KEY,
                TypeKey NVARCHAR(50) NOT NULL
            );

            CREATE TABLE dbo.Transactions
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                TenantId INT NOT NULL,
                CustomerId INT NOT NULL,
                TransactionTypeId INT NOT NULL,
                ReferenceId INT NOT NULL,
                TransactionNumber NVARCHAR(50) NOT NULL,
                TransactionDate DATETIME2(0) NOT NULL,
                TotalAmount DECIMAL(19,4) NULL,
                PaidAmount DECIMAL(19,4) NULL,
                PaymentStatus NVARCHAR(40) NULL,
                IsReturn BIT NOT NULL,
                CounterCurrencyCode NVARCHAR(10) NULL
            );
            """);
    }

    /// <summary>
    /// Two tenants. Tenant 1 owns customer id 1 with three sale invoices; tenant 2 owns
    /// customer id 2 with none. Returns the customer ids so tests stay independent of IDENTITY seeds.
    /// </summary>
    public async Task<(int Tenant1CustomerId, int Tenant2CustomerId)> SeedTwoTenantsAsync()
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.TransactionTypes (Id, TypeKey) VALUES (1, 'sale');
            """);

        var tenant1CustomerId = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO dbo.Customers (TenantId, Name, Phone, Email, Address)
            VALUES (1, 'Tenant One Client', '111-1111', 't1@example.com', 'Street 1');
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """);

        var tenant2CustomerId = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO dbo.Customers (TenantId, Name, Phone, Email, Address)
            VALUES (2, 'Tenant Two Client', '222-2222', 't2@example.com', 'Street 2');
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """);

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Transactions
                (TenantId, CustomerId, TransactionTypeId, ReferenceId, TransactionNumber, TransactionDate,
                 TotalAmount, PaidAmount, PaymentStatus, IsReturn, CounterCurrencyCode)
            VALUES
                (1, @CustomerId, 1, 101, 'INV-101', '2026-06-01T10:00:00', 300.00, 300.00, 'Paid',        0, 'USD'),
                (1, @CustomerId, 1, 102, 'INV-102', '2026-06-02T10:00:00', 200.00, 000.00, 'Unpaid',      0, 'USD'),
                (1, @CustomerId, 1, 103, 'INV-103', '2026-06-03T10:00:00', 150.00, 050.00, 'PartiallyPaid', 0, 'USD');
            """,
            new { CustomerId = tenant1CustomerId });

        return (tenant1CustomerId, tenant2CustomerId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
