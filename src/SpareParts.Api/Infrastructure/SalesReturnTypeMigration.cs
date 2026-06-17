using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class SalesReturnTypeMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();

        conn.Execute(
            @"IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = N'sale_return')
BEGIN
    DECLARE @MaxSort INT;
    SELECT @MaxSort = ISNULL(MAX(SortOrder), 0) FROM dbo.TransactionTypes;

    INSERT INTO dbo.TransactionTypes
    (
        Name,
        TypeKey,
        CurrencyCode,
        CounterRate,
        IsActive,
        SortOrder,
        SerialNumberFormat,
        SerialStartNumber,
        SerialCurrentNumber
    )
    VALUES
    (
        N'Sales Return',
        N'sale_return',
        N'USD',
        1,
        1,
        @MaxSort + 10,
        N'RET-{DATE:yyyyMMdd}-{NUMBER:00000000}',
        1,
        0
    );
END;");
    }
}
