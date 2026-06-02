using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class CustomerPriceTierMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF COL_LENGTH('dbo.Customers', 'PriceTier') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD PriceTier INT NOT NULL CONSTRAINT DF_Customers_PriceTier DEFAULT 0;
END;
""");
    }
}
