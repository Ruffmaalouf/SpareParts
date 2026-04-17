using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarPurchasesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarPurchases
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarPurchases PRIMARY KEY,
        PurchaseNumber NVARCHAR(20) NOT NULL,
        UsedCarId INT NOT NULL,
        SupplierId INT NOT NULL,
        PurchaseDate DATETIME2(0) NOT NULL,
        BaseCurrencyCode CHAR(3) NOT NULL,
        TotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_TotalBaseAmount DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaidAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaymentStatus DEFAULT (N'Unpaid'),
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarPurchases_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarPurchases_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars(Id),
        CONSTRAINT FK_UsedCarPurchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UsedCarPurchases_PurchaseNumber'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
BEGIN
    CREATE UNIQUE INDEX UX_UsedCarPurchases_PurchaseNumber
        ON dbo.UsedCarPurchases(PurchaseNumber);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarPurchases_UsedCarId'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
BEGIN
    CREATE INDEX IX_UsedCarPurchases_UsedCarId
        ON dbo.UsedCarPurchases(UsedCarId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarPurchases_SupplierId'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
BEGIN
    CREATE INDEX IX_UsedCarPurchases_SupplierId
        ON dbo.UsedCarPurchases(SupplierId);
END;

IF OBJECT_ID('dbo.UsedCarPurchaseLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarPurchaseLines
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarPurchaseLines PRIMARY KEY,
        UsedCarPurchaseId INT NOT NULL,
        DetailKey NVARCHAR(80) NOT NULL,
        Description NVARCHAR(160) NOT NULL,
        Amount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_Amount DEFAULT (0),
        CurrencyCode CHAR(3) NOT NULL,
        RateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_RateToBase DEFAULT (1),
        BaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_BaseAmount DEFAULT (0),
        AccountId INT NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_SortOrder DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarPurchaseLines_UsedCarPurchases FOREIGN KEY (UsedCarPurchaseId) REFERENCES dbo.UsedCarPurchases(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UsedCarPurchaseLines_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id)
    );
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarPurchaseLines_UsedCarPurchaseId'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchaseLines'))
BEGIN
    CREATE INDEX IX_UsedCarPurchaseLines_UsedCarPurchaseId
        ON dbo.UsedCarPurchaseLines(UsedCarPurchaseId, SortOrder, Id);
END;");
    }
}
