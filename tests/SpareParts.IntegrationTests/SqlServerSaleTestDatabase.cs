using System.Data;
using Dapper;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.MsSql;

namespace SpareParts.IntegrationTests;

internal sealed class SqlServerSaleTestDatabase : IAsyncDisposable
{
    private MsSqlContainer? _container;

    public string? ConnectionString { get; private set; }
    public bool IsAvailable { get; private set; }
    public string? AvailabilityReason { get; private set; }

    public bool CanRunIntegrationTests()
    {
        if (IsAvailable)
        {
            return true;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip.Always($"Docker unavailable in CI — SQL Server container could not start. {AvailabilityReason}");
        }

        return false;
    }

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
            IF OBJECT_ID('dbo.JournalLines', 'U') IS NOT NULL DROP TABLE dbo.JournalLines;
            IF OBJECT_ID('dbo.JournalEntries', 'U') IS NOT NULL DROP TABLE dbo.JournalEntries;
            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL DROP TABLE dbo.StockMovements;
            IF OBJECT_ID('dbo.Stock', 'U') IS NOT NULL DROP TABLE dbo.Stock;
            IF OBJECT_ID('dbo.SalesInvoiceItems', 'U') IS NOT NULL DROP TABLE dbo.SalesInvoiceItems;
            IF OBJECT_ID('dbo.SalesInvoices', 'U') IS NOT NULL DROP TABLE dbo.SalesInvoices;
            IF OBJECT_ID('dbo.AccountingPostingSettings', 'U') IS NOT NULL DROP TABLE dbo.AccountingPostingSettings;
            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL DROP TABLE dbo.Parts;
            IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL DROP TABLE dbo.Accounts;
            IF OBJECT_ID('dbo.SalesInvoiceNumberSequence', 'SO') IS NOT NULL DROP SEQUENCE dbo.SalesInvoiceNumberSequence;

            CREATE SEQUENCE dbo.SalesInvoiceNumberSequence AS BIGINT START WITH 1 INCREMENT BY 1;

            CREATE TABLE dbo.Accounts
            (
                Id INT NOT NULL PRIMARY KEY,
                Code NVARCHAR(20) NOT NULL,
                Name NVARCHAR(120) NOT NULL
            );

            CREATE TABLE dbo.AccountingPostingSettings
            (
                SettingKey NVARCHAR(80) NOT NULL PRIMARY KEY,
                AccountId INT NOT NULL
            );

            CREATE TABLE dbo.Parts
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                InternalCode NVARCHAR(50) NOT NULL,
                Name NVARCHAR(120) NOT NULL,
                CostPrice DECIMAL(19,4) NOT NULL,
                SalePrice DECIMAL(19,4) NOT NULL,
                IsActive BIT NOT NULL
            );

            CREATE TABLE dbo.SalesInvoices
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                InvoiceNumber NVARCHAR(50) NOT NULL,
                InvoiceDate DATETIME2(0) NOT NULL,
                CustomerId INT NULL,
                WarehouseId INT NOT NULL,
                Subtotal DECIMAL(19,4) NOT NULL,
                DiscountAmount DECIMAL(19,4) NOT NULL,
                TaxAmount DECIMAL(19,4) NOT NULL,
                TotalAmount DECIMAL(19,4) NOT NULL,
                PaidAmount DECIMAL(19,4) NOT NULL,
                PaymentStatus NVARCHAR(40) NOT NULL,
                PaymentMethod NVARCHAR(50) NULL,
                Notes NVARCHAR(1000) NULL,
                IsReturn BIT NOT NULL,
                ParentInvoiceId INT NULL,
                TotalCost DECIMAL(19,4) NOT NULL,
                CreatedAt DATETIME2(0) NOT NULL,
                CreatedByUserId INT NULL
            );

            CREATE TABLE dbo.SalesInvoiceItems
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                InvoiceId INT NOT NULL,
                PartId INT NOT NULL,
                Quantity INT NOT NULL,
                UnitPrice DECIMAL(19,4) NOT NULL,
                DiscountAmount DECIMAL(19,4) NOT NULL,
                TaxRate DECIMAL(19,4) NOT NULL,
                LineTotal DECIMAL(19,4) NOT NULL,
                CreatedAt DATETIME2(0) NOT NULL,
                CreatedByUserId INT NULL
            );

            CREATE TABLE dbo.Stock
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                PartId INT NOT NULL,
                WarehouseId INT NOT NULL,
                LocationId INT NULL,
                Quantity INT NOT NULL,
                ReservedQuantity INT NOT NULL,
                CreatedAt DATETIME2(0) NOT NULL,
                CreatedByUserId INT NULL,
                ModifiedAt DATETIME2(0) NULL,
                ModifiedByUserId INT NULL
            );

            CREATE TABLE dbo.StockMovements
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                PartId INT NOT NULL,
                WarehouseId INT NOT NULL,
                Quantity INT NOT NULL,
                MovementType INT NOT NULL,
                ReferenceType NVARCHAR(50) NOT NULL,
                ReferenceId INT NULL,
                UnitCost DECIMAL(19,4) NOT NULL,
                CreatedAt DATETIME2(0) NOT NULL,
                CreatedByUserId INT NULL
            );

            CREATE TABLE dbo.JournalEntries
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                EntryDate DATETIME2(0) NOT NULL,
                ReferenceType NVARCHAR(50) NULL,
                ReferenceId INT NULL,
                Description NVARCHAR(255) NULL,
                CreatedAt DATETIME2(0) NOT NULL,
                CreatedByUserId INT NULL
            );

            CREATE TABLE dbo.JournalLines
            (
                Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                JournalEntryId INT NOT NULL,
                AccountId INT NOT NULL,
                Debit DECIMAL(19,4) NOT NULL,
                Credit DECIMAL(19,4) NOT NULL,
                CreatedAt DATETIME2(0) NOT NULL,
                CreatedByUserId INT NULL,
                CONSTRAINT FK_JournalLines_JournalEntries FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(Id),
                CONSTRAINT FK_JournalLines_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id)
            );
            """);
    }

    public async Task SeedBaselineAsync(bool seedValidPostingAccounts = true, int openingStock = 5)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(ConnectionString))
        {
            return;
        }

        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO dbo.Parts (InternalCode, Name, CostPrice, SalePrice, IsActive)
            VALUES ('P-001', 'Brake Pad', 10.00, 20.00, 1);

            INSERT INTO dbo.Stock (PartId, WarehouseId, LocationId, Quantity, ReservedQuantity, CreatedAt, CreatedByUserId)
            VALUES (1, 1, NULL, @OpeningStock, 0, SYSUTCDATETIME(), 1);
            """,
            new { OpeningStock = openingStock });

        if (seedValidPostingAccounts)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.Accounts (Id, Code, Name)
                VALUES (1, '1000', 'Cash'),
                       (2, '1100', 'Inventory'),
                       (5, '4000', 'Sales Revenue'),
                       (6, '5000', 'COGS');

                INSERT INTO dbo.AccountingPostingSettings (SettingKey, AccountId)
                VALUES ('sales_cash', 1),
                       ('sales_revenue', 5),
                       ('cogs', 6),
                       ('inventory', 2),
                       ('purchase_offset', 1);
                """);
        }
        else
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO dbo.AccountingPostingSettings (SettingKey, AccountId)
                VALUES ('sales_cash', 9999),
                       ('sales_revenue', 9999),
                       ('cogs', 9999),
                       ('inventory', 9999),
                       ('purchase_offset', 9999);
                """);
        }
    }

    public async Task<int> ScalarIntAsync(string sql)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(sql);
    }

    public async Task<int> GetStockQuantityAsync(int partId, int warehouseId)
    {
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(ConnectionString);
        await connection.OpenAsync();
        return await connection.ExecuteScalarAsync<int>(
            "SELECT Quantity FROM dbo.Stock WHERE PartId = @PartId AND WarehouseId = @WarehouseId;",
            new { PartId = partId, WarehouseId = warehouseId });
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }
}
