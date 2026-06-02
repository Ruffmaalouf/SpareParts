using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartExpiryMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF COL_LENGTH('dbo.Parts', 'ExpiryDate') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD ExpiryDate DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.Parts', 'ShelfLifeDays') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD ShelfLifeDays INT NULL;
END;
""");
    }
}
