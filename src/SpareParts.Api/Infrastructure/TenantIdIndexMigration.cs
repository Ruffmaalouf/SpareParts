using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds TenantId indexes for high-volume tenant-owned child tables that
/// <see cref="TenantIdMigration"/> marks with <c>CreateIndex = false</c> (i.e. it adds/backfills
/// the TenantId column, but does not index it).
/// </summary>
/// <remarks>
/// Scoped to the two tables that are both (a) explicitly high-volume — every invoice/journal
/// posting inserts rows here — and (b) directly queried by TenantId in repository/service code
/// today (see <c>JournalRepository</c>, <c>OwnerCockpitService</c>, <c>SalesRepository</c>,
/// <c>PurchasesRepository</c>, <c>BusinessAssistantService</c>, etc.):
/// <list type="bullet">
///   <item><description>dbo.JournalLines</description></item>
///   <item><description>dbo.TransactionItems</description></item>
/// </list>
/// Intentionally does NOT touch <see cref="TenantIdMigration"/>'s own table list/history — this is
/// a separate, additive, idempotent migration (IF NOT EXISTS on sys.indexes) that can run after it.
/// Must run after <see cref="TenantIdMigration"/> so the TenantId column already exists.
/// </remarks>
public static class TenantIdIndexMigration
{
    private static readonly string[] Tables =
    [
        "JournalLines",
        "TransactionItems"
    ];

    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();

        foreach (var tableName in Tables)
        {
            var qualifiedTable = $"dbo.{tableName}";

            var tableExists = conn.ExecuteScalar<int>(
                "SELECT CASE WHEN OBJECT_ID(@TableName, 'U') IS NULL THEN 0 ELSE 1 END;",
                new { TableName = qualifiedTable }) == 1;
            if (!tableExists)
            {
                continue;
            }

            var tenantColumnExists = conn.ExecuteScalar<int>(
                "SELECT CASE WHEN COL_LENGTH(@TableName, 'TenantId') IS NULL THEN 0 ELSE 1 END;",
                new { TableName = qualifiedTable }) == 1;
            if (!tenantColumnExists)
            {
                // TenantIdMigration hasn't added the column yet (unexpected ordering) — skip for
                // now; it will be picked up on a later startup once the column exists.
                continue;
            }

            var indexName = $"IX_{tableName}_TenantId";

            var indexExists = conn.ExecuteScalar<int>(
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
                new { IndexName = indexName, TableName = qualifiedTable }) == 1;

            if (!indexExists)
            {
                conn.Execute(
                    $"CREATE NONCLUSTERED INDEX [{indexName}] ON {qualifiedTable} (TenantId);",
                    commandTimeout: 120);
            }
        }
    }
}
