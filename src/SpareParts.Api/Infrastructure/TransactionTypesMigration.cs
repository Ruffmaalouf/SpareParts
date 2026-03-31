using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class TransactionTypesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.TransactionTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionTypes
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL UNIQUE,
        CurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_TransactionTypes_Currency DEFAULT ('USD'),
        CounterRate DECIMAL(19, 8) NOT NULL CONSTRAINT CK_TransactionTypes_CounterRate_Positive CHECK (CounterRate > 0),
        IsActive BIT NOT NULL CONSTRAINT DF_TransactionTypes_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TransactionTypes_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes)
BEGIN
    INSERT INTO dbo.TransactionTypes (Name, CurrencyCode, CounterRate, IsActive)
    VALUES
        ('Sales', 'USD', 1, 1),
        ('Purchases', 'USD', 1, 1);
END;");
    }
}
