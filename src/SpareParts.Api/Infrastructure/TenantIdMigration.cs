using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds TenantId columns to tenant-owned tables, backfills existing rows to the
/// default tenant, and creates useful tenant lookup indexes.
/// Must run after TenantsMigration and after schema/table migrations.
/// </summary>
public static class TenantIdMigration
{
    private static readonly TenantTable[] Tables =
    [
        new("Users", true),
        new("Parts", true),
        new("Stock", true),
        new("StockMovements", true),
        new("Customers", true),
        new("Suppliers", true),
        new("Warehouses", true),
        new("Location", false),
        new("Locations", true),
        new("Brands", true),
        new("Categories", true),
        new("CarBrands", false),
        new("CarModels", false),
        new("UsedCars", true),
        new("usedcar_images", false),
        new("Transactions", true),
        new("TransactionItems", false),
        new("TransactionTypes", true),
        new("Accounts", true),
        new("AccountingPostingSettings", false),
        new("JournalEntries", true),
        new("JournalLines", false),
        new("UsedCarPurchases", true),
        new("UsedCarPurchaseLines", false),
        new("UsedCarWholesaleSales", true),
        new("Quotes", false),
        new("QuoteItems", false),
        new("PartRequests", false),
        new("PartRequestReservations", false),
        new("PartSubstitutes", false),
        new("ReorderRules", false),
        new("OutboundMessages", false),
        new("WhatsAppCampaigns", false),
        new("WhatsAppCampaignRecipients", false),
        new("ReportBuilderSavedReports", false),
        new("ReportBuilderFavoriteReports", false),
        new("ReportBuilderTableLinks", false),
        new("Shipments", false),
        new("LoyaltyTransactions", false),
        new("WarrantyClaims", false),
        new("SupplierPriceHistory", false),
        new("ActivityLogs", false),
        new("CurrencyRates", false),
        new("AppConstants", false),
        new("ExcelImportMetadata", false)
    ];

    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();

        foreach (var table in Tables)
        {
            if (!TableExists(conn, table.Name))
            {
                continue;
            }

            var quotedTable = QuoteTable(table.Name);
            if (!TenantColumnExists(conn, table.Name))
            {
                conn.Execute($"ALTER TABLE {quotedTable} ADD TenantId INT NULL;", commandTimeout: 120);
            }

            conn.Execute($"UPDATE {quotedTable} SET TenantId = @TenantId WHERE TenantId IS NULL;",
                new { TenantId = TenantsMigration.DefaultTenantId },
                commandTimeout: 120);

            if (table.CreateIndex)
            {
                EnsureTenantIndex(conn, table.Name, quotedTable);
            }
        }
    }

    private static bool TableExists(System.Data.IDbConnection conn, string tableName)
        => conn.ExecuteScalar<int>(
            "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END;",
            new { TableName = $"dbo.{tableName}" }) == 1;

    private static bool TenantColumnExists(System.Data.IDbConnection conn, string tableName)
        => conn.ExecuteScalar<int>(
            "SELECT CASE WHEN COL_LENGTH(@TableName, 'TenantId') IS NULL THEN 0 ELSE 1 END;",
            new { TableName = $"dbo.{tableName}" }) == 1;

    private static void EnsureTenantIndex(System.Data.IDbConnection conn, string tableName, string quotedTable)
    {
        var indexName = $"IX_{tableName}_TenantId";
        var safeIndexName = QuoteIdentifier(indexName);

        var exists = conn.ExecuteScalar<int>(
            """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE name = @IndexName
                      AND object_id = OBJECT_ID(@TableName)
                )
                THEN 1 ELSE 0
            END;
            """,
            new
            {
                IndexName = indexName,
                TableName = $"dbo.{tableName}"
            }) == 1;

        if (!exists)
        {
            conn.Execute(
                $"CREATE NONCLUSTERED INDEX {safeIndexName} ON {quotedTable} (TenantId);",
                commandTimeout: 120);
        }
    }

    private static string QuoteTable(string tableName)
        => $"dbo.{QuoteIdentifier(tableName)}";

    private static string QuoteIdentifier(string value)
        => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";
}
