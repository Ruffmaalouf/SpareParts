using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Tracks which of the C# startup migrations (see <see cref="SpareParts.Api.Hosting.SparePartsApiComposition.MigrationNames"/>)
/// have already been applied, so that <c>RunMigrations</c> can skip re-executing a migration's
/// full body on every API startup once it has successfully run once.
/// </summary>
/// <remarks>
/// This is purely a startup-time optimization. Each migration's own internal idempotency guards
/// (IF OBJECT_ID(...) IS NULL, IF NOT EXISTS(...), etc.) are left in place as a safety net — if a
/// migration is ever re-run (e.g. the ledger row is missing, or a migration is applied to more
/// than one connection/database), it must still be safe to execute again.
/// </remarks>
public static class SchemaMigrationLedger
{
    private const string TableName = "dbo.SchemaMigrations";

    /// <summary>
    /// Creates the ledger table if it does not already exist. Must be called once before any
    /// <see cref="IsApplied"/>/<see cref="MarkApplied"/> calls.
    /// </summary>
    public static void EnsureTableExists(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute($"""
            IF OBJECT_ID('{TableName}', 'U') IS NULL
            BEGIN
                CREATE TABLE {TableName}
                (
                    Id            INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    Name          NVARCHAR(200)      NOT NULL,
                    AppliedAtUtc  DATETIME2(0)        NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
                    CONSTRAINT UQ_SchemaMigrations_Name UNIQUE (Name)
                );
            END;
            """);
    }

    public static bool IsApplied(ISqlConnectionFactory factory, string migrationName)
    {
        using var conn = factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {TableName} WHERE Name = @Name) THEN 1 ELSE 0 END;",
            new { Name = migrationName }) == 1;
    }

    public static void MarkApplied(ISqlConnectionFactory factory, string migrationName)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            $"""
            IF NOT EXISTS (SELECT 1 FROM {TableName} WHERE Name = @Name)
            BEGIN
                INSERT INTO {TableName} (Name, AppliedAtUtc) VALUES (@Name, SYSUTCDATETIME());
            END;
            """,
            new { Name = migrationName });
    }

    /// <summary>
    /// Runs <paramref name="ensureApplied"/> only if <paramref name="migrationName"/> is not yet
    /// recorded in the ledger, then records it as applied. If the migration throws, nothing is
    /// recorded so it will be retried on the next startup.
    /// </summary>
    public static void RunOnce(ISqlConnectionFactory factory, string migrationName, Action<ISqlConnectionFactory> ensureApplied)
    {
        if (IsApplied(factory, migrationName))
        {
            return;
        }

        ensureApplied(factory);
        MarkApplied(factory, migrationName);
    }
}
