using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartAveragePriceMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Parts', 'AveragePrice') IS NULL
            BEGIN
                ALTER TABLE dbo.Parts
                ADD AveragePrice DECIMAL(19, 4) NULL;
            END;
            """);
    }
}
