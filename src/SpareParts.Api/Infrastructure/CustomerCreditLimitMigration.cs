using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class CustomerCreditLimitMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF COL_LENGTH('dbo.Customers', 'CreditLimit') IS NULL
    ALTER TABLE dbo.Customers ADD CreditLimit DECIMAL(19,4) NOT NULL CONSTRAINT DF_Customers_CreditLimit DEFAULT 0;
""");
    }
}
