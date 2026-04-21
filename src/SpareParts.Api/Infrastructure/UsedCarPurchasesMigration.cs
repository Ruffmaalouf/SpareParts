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
        PurchaseNumber NVARCHAR(32) NOT NULL,
        UsedCarId INT NOT NULL,
        SupplierId INT NOT NULL,
        PurchaseDate DATETIME2(0) NOT NULL,
        BaseCurrencyCode CHAR(3) NOT NULL,
        CounterCurrencyCode CHAR(3) NOT NULL,
        TotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_TotalBaseAmount DEFAULT (0),
        TotalCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_TotalCounterAmount DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaidAmount DEFAULT (0),
        PaidCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaidCounterAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaymentStatus DEFAULT (N'Unpaid'),
        PostingStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarPurchases_PostingStatus DEFAULT (N'Draft'),
        PostedAt DATETIME2(0) NULL,
        PostedByUserId INT NULL,
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarPurchases_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarPurchases_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars(Id),
        CONSTRAINT FK_UsedCarPurchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id)
    );
END;

IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCarPurchases', 'PurchaseNumber') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCarPurchases', 'PurchaseNumber') < 64
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ALTER COLUMN PurchaseNumber NVARCHAR(32) NOT NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'CounterCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD CounterCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'TotalCounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD TotalCounterAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PaidCounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PaidCounterAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PostingStatus') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PostingStatus NVARCHAR(20) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PostedAt') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PostedAt DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PostedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PostedByUserId INT NULL;
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
        CounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_CounterAmount DEFAULT (0),
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

IF COL_LENGTH('dbo.UsedCarPurchaseLines', 'CounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchaseLines ADD CounterAmount DECIMAL(19, 4) NULL;
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

        conn.Execute(
            @"

DECLARE @UsedCarPurchasesDefaultCounterCurrencyCode CHAR(3) = 'USD';

IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    SELECT TOP (1) @UsedCarPurchasesDefaultCounterCurrencyCode = UPPER(LTRIM(RTRIM(Value)))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode'
      AND LEN(LTRIM(RTRIM(Value))) = 3;
END;

SET @UsedCarPurchasesDefaultCounterCurrencyCode = COALESCE(NULLIF(@UsedCarPurchasesDefaultCounterCurrencyCode, ''), 'USD');

UPDATE p
SET CounterCurrencyCode = COALESCE(NULLIF(uc.CounterCurrencyCode, ''), @UsedCarPurchasesDefaultCounterCurrencyCode),
    TotalCounterAmount = CASE
        WHEN uc.CounterRateToBase > 0 THEN ROUND(p.TotalBaseAmount / uc.CounterRateToBase, 4)
        ELSE p.TotalBaseAmount
    END,
    PaidCounterAmount = CASE
        WHEN uc.CounterRateToBase > 0 THEN ROUND(p.PaidAmount / uc.CounterRateToBase, 4)
        ELSE p.PaidAmount
    END
FROM dbo.UsedCarPurchases p
LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
WHERE p.CounterCurrencyCode IS NULL
   OR p.TotalCounterAmount IS NULL
   OR p.PaidCounterAmount IS NULL;

UPDATE p
SET PostingStatus = CASE
        WHEN je.CreatedAt IS NULL THEN N'Draft'
        ELSE N'Posted'
    END,
    PostedAt = CASE
        WHEN je.CreatedAt IS NULL THEN NULL
        ELSE COALESCE(p.PostedAt, je.CreatedAt)
    END,
    PostedByUserId = CASE
        WHEN je.CreatedAt IS NULL THEN NULL
        ELSE COALESCE(p.PostedByUserId, je.CreatedByUserId)
    END
FROM dbo.UsedCarPurchases p
OUTER APPLY
(
    SELECT TOP (1) je.CreatedAt,
                   je.CreatedByUserId
    FROM dbo.JournalEntries je
    WHERE je.ReferenceType = N'UsedCarPurchase'
      AND je.ReferenceId = p.Id
    ORDER BY je.Id DESC
) je
WHERE p.PostingStatus IS NULL
   OR LTRIM(RTRIM(p.PostingStatus)) = N'';

BEGIN TRY
    ALTER TABLE dbo.UsedCarPurchases ALTER COLUMN CounterCurrencyCode CHAR(3) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.UsedCarPurchases ALTER COLUMN TotalCounterAmount DECIMAL(19, 4) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.UsedCarPurchases ALTER COLUMN PaidCounterAmount DECIMAL(19, 4) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.UsedCarPurchases ALTER COLUMN PostingStatus NVARCHAR(20) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

UPDATE l
SET CounterAmount = CASE
    WHEN l.DetailKey = 'used_car_price' THEN ISNULL(uc.PriceCounter, 0)
    WHEN l.DetailKey = 'used_car_transportation' THEN ISNULL(uc.Transportation, 0)
    WHEN l.DetailKey = 'used_car_partout' THEN ISNULL(uc.PartOutAmount, 0)
    WHEN l.DetailKey = 'used_car_shipping' THEN ISNULL(uc.Shipping, 0)
    WHEN l.DetailKey = 'used_car_customs' THEN ISNULL(uc.Customs, 0)
    WHEN uc.CounterRateToBase > 0 THEN ROUND(l.BaseAmount / uc.CounterRateToBase, 4)
    ELSE l.BaseAmount
END
FROM dbo.UsedCarPurchaseLines l
INNER JOIN dbo.UsedCarPurchases p ON p.Id = l.UsedCarPurchaseId
LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
WHERE l.CounterAmount IS NULL;

BEGIN TRY
    ALTER TABLE dbo.UsedCarPurchaseLines ALTER COLUMN CounterAmount DECIMAL(19, 4) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;");
    }
}
