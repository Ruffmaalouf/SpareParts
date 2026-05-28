using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarWholesaleSalesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.UsedCarWholesaleSaleNumberSequence', 'SO') IS NULL
BEGIN
    CREATE SEQUENCE dbo.UsedCarWholesaleSaleNumberSequence
        AS INT
        START WITH 1
        INCREMENT BY 1;
END;

IF OBJECT_ID('dbo.UsedCarWholesaleSales', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarWholesaleSales
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarWholesaleSales PRIMARY KEY,
        SaleNumber NVARCHAR(32) NOT NULL,
        UsedCarId INT NOT NULL,
        CustomerId INT NULL,
        BuyerName NVARCHAR(160) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_BuyerName DEFAULT (N''),
        BuyerPhone NVARCHAR(60) NULL,
        SaleDate DATETIME2(0) NOT NULL,
        CurrencyCode CHAR(3) NOT NULL,
        SalePrice DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SalePrice DEFAULT (0),
        SaleRateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SaleRateToBase DEFAULT (1),
        SalePriceBase DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SalePriceBase DEFAULT (0),
        CounterCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_CounterCurrencyCode DEFAULT ('USD'),
        CounterRateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_CounterRateToBase DEFAULT (1),
        SalePriceCounter DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SalePriceCounter DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaidAmount DEFAULT (0),
        PaidBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaidBaseAmount DEFAULT (0),
        PaidCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaidCounterAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaymentStatus DEFAULT (N'Unpaid'),
        IsForParts BIT NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_IsForParts DEFAULT (0),
        RepairItemsJson NVARCHAR(MAX) NULL,
        RepairTotalAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalAmount DEFAULT (0),
        RepairTotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalBaseAmount DEFAULT (0),
        RepairTotalCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalCounterAmount DEFAULT (0),
        PaymentMethod NVARCHAR(80) NULL,
        Notes NVARCHAR(800) NULL,
        SoldAsIsAcknowledged BIT NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SoldAsIsAcknowledged DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarWholesaleSales_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars(Id)
    );
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'IsForParts') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD IsForParts BIT NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_IsForParts DEFAULT (0);
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairItemsJson') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairItemsJson NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairTotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairTotalAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalAmount DEFAULT (0);
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairTotalBaseAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairTotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalBaseAmount DEFAULT (0);
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairTotalCounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairTotalCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalCounterAmount DEFAULT (0);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UsedCarWholesaleSales_SaleNumber'
      AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    CREATE UNIQUE INDEX UX_UsedCarWholesaleSales_SaleNumber
        ON dbo.UsedCarWholesaleSales(SaleNumber);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UsedCarWholesaleSales_UsedCarId'
      AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    CREATE UNIQUE INDEX UX_UsedCarWholesaleSales_UsedCarId
        ON dbo.UsedCarWholesaleSales(UsedCarId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarWholesaleSales_CustomerId'
      AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    CREATE INDEX IX_UsedCarWholesaleSales_CustomerId
        ON dbo.UsedCarWholesaleSales(CustomerId);
END;

IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_UsedCarWholesaleSales_Customers'
         AND parent_object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales WITH NOCHECK
        ADD CONSTRAINT FK_UsedCarWholesaleSales_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id);
END;

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_UsedCarWholesaleSales_CreatedByUsers'
         AND parent_object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales WITH NOCHECK
        ADD CONSTRAINT FK_UsedCarWholesaleSales_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id);
END;");
    }
}
