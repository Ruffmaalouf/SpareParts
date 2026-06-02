using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class CustomerLoyaltyMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.LoyaltyTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoyaltyTransactions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoyaltyTransactions PRIMARY KEY,
        CustomerId INT NOT NULL,
        Points INT NOT NULL,
        TransactionType NVARCHAR(50) NOT NULL CONSTRAINT DF_LoyaltyTransactions_Type DEFAULT N'Adjustment',
        ReferenceType NVARCHAR(50) NULL,
        ReferenceId INT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LoyaltyTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_LoyaltyTransactions_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;

IF COL_LENGTH('dbo.Customers', 'LoyaltyPoints') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD LoyaltyPoints INT NOT NULL CONSTRAINT DF_Customers_LoyaltyPoints DEFAULT 0;
END;
""");
    }
}
