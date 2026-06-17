using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class NewVsUsedPricingMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Parts') AND name = 'OemNewPrice')
BEGIN
    ALTER TABLE dbo.Parts ADD OemNewPrice DECIMAL(18,4) NULL;
END;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Parts') AND name = 'AftermarketNewPrice')
BEGIN
    ALTER TABLE dbo.Parts ADD AftermarketNewPrice DECIMAL(18,4) NULL;
END;
""");
    }
}
