using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class CurrencyRatesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.CurrencyRates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CurrencyRates
    (
        Code CHAR(3) NOT NULL PRIMARY KEY,
        RateToUsd DECIMAL(19, 8) NOT NULL CONSTRAINT CK_CurrencyRates_PositiveRate CHECK (RateToUsd > 0),
        BaseCode CHAR(3) NOT NULL CONSTRAINT DF_CurrencyRates_BaseCode DEFAULT ('USD'),
        SnapshotUtc DATETIME2(0) NOT NULL CONSTRAINT DF_CurrencyRates_SnapshotUtc DEFAULT SYSUTCDATETIME()
    );
END;");
    }
}
