using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.UsedCars', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCars
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCars PRIMARY KEY,
        SupplierId INT NULL,
        CarModelId INT NOT NULL,
        ModelYear INT NOT NULL,
        PriceCurrency CHAR(3) NOT NULL,
        Price DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_Price_Positive CHECK (Price > 0),
        PriceBase DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_PriceBase_NonNegative CHECK (PriceBase >= 0),
        PriceCounter DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_PriceCounter_NonNegative CHECK (PriceCounter >= 0),
        LocationId INT NULL,
        Location NVARCHAR(160) NOT NULL CONSTRAINT DF_UsedCars_Location DEFAULT (N''),
        Transportation DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Transportation DEFAULT (0),
        IsReceived BIT NOT NULL CONSTRAINT DF_UsedCars_IsReceived DEFAULT (0),
        IsShipped BIT NOT NULL CONSTRAINT DF_UsedCars_IsShipped DEFAULT (0),
        PartOutAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_PartOutAmount DEFAULT (0),
        Shipping DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Shipping DEFAULT (0),
        Customs DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Customs DEFAULT (0),
        TotalBeforeShipping DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_TotalBeforeShipping DEFAULT (0),
        GrandTotalBase DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_GrandTotalBase DEFAULT (0),
        GrandTotalCounter DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_GrandTotalCounter DEFAULT (0),
        BaseCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_UsedCars_BaseCurrencyCode DEFAULT ('USD'),
        CounterCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_UsedCars_CounterCurrencyCode DEFAULT ('USD'),
        CounterRateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCars_CounterRateToBase DEFAULT (1),
        ReceivedAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCars_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt DATETIME2(0) NULL,
        CreatedByUserId INT NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCars_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id),
        CONSTRAINT FK_UsedCars_CarModels FOREIGN KEY (CarModelId) REFERENCES dbo.CarModels (Id),
        CONSTRAINT FK_UsedCars_Location FOREIGN KEY (LocationId) REFERENCES dbo.Location (LocationId),
        CONSTRAINT FK_UsedCars_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_UsedCars_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_UsedCars_SupplierId ON dbo.UsedCars (SupplierId);
    CREATE INDEX IX_UsedCars_CarModelId ON dbo.UsedCars (CarModelId);
    CREATE INDEX IX_UsedCars_LocationId ON dbo.UsedCars (LocationId);
END;

IF COL_LENGTH('dbo.UsedCars', 'SupplierId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD SupplierId INT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'LocationId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD LocationId INT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'IsReceived') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD IsReceived BIT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'IsShipped') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD IsShipped BIT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'PartOutAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD PartOutAmount DECIMAL(18, 2) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'BaseCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD BaseCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'CounterCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD CounterCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'CounterRateToBase') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD CounterRateToBase DECIMAL(19, 8) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'ReceivedAt') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD ReceivedAt DATETIME2(0) NULL;
END;
");

        conn.Execute(
            @"
UPDATE dbo.UsedCars
SET IsReceived = CASE WHEN ISNULL(Customs, 0) > 0 THEN 1 ELSE 0 END
WHERE IsReceived IS NULL;

BEGIN TRY
    ALTER TABLE dbo.UsedCars ALTER COLUMN IsReceived BIT NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

UPDATE dbo.UsedCars
SET IsShipped = 0
WHERE IsShipped IS NULL;

BEGIN TRY
    ALTER TABLE dbo.UsedCars ALTER COLUMN IsShipped BIT NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

IF COL_LENGTH('dbo.UsedCars', 'PartOut') IS NOT NULL
BEGIN
    UPDATE dbo.UsedCars
    SET PartOutAmount = COALESCE(
        TRY_CONVERT(DECIMAL(18, 2), NULLIF(LTRIM(RTRIM(CONVERT(NVARCHAR(160), PartOut))), N'')),
        0
    )
    WHERE PartOutAmount IS NULL;
END
ELSE
BEGIN
    UPDATE dbo.UsedCars
    SET PartOutAmount = 0
    WHERE PartOutAmount IS NULL;
END;

BEGIN TRY
    ALTER TABLE dbo.UsedCars ALTER COLUMN PartOutAmount DECIMAL(18, 2) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

DECLARE @UsedCarsBaseCurrencyCode CHAR(3) = 'USD';
DECLARE @UsedCarsCounterCurrencyCode CHAR(3) = 'USD';
DECLARE @UsedCarsCounterRateToBase DECIMAL(19, 8) = 1;

IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    SELECT TOP (1) @UsedCarsBaseCurrencyCode = UPPER(LTRIM(RTRIM(Value)))
    FROM dbo.AppConstants
    WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode')
      AND LEN(LTRIM(RTRIM(Value))) = 3
    ORDER BY CASE WHEN [Key] = 'BaseCurrencyCode' THEN 0 ELSE 1 END;

    SELECT TOP (1) @UsedCarsCounterCurrencyCode = UPPER(LTRIM(RTRIM(Value)))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode'
      AND LEN(LTRIM(RTRIM(Value))) = 3;

    SELECT TOP (1) @UsedCarsCounterRateToBase = TRY_CONVERT(DECIMAL(19, 8), Value)
    FROM dbo.AppConstants
    WHERE [Key] = 'DefaultCounterRate'
      AND TRY_CONVERT(DECIMAL(19, 8), Value) > 0;
END;

SET @UsedCarsCounterCurrencyCode = COALESCE(NULLIF(@UsedCarsCounterCurrencyCode, ''), @UsedCarsBaseCurrencyCode, 'USD');
SET @UsedCarsBaseCurrencyCode = COALESCE(NULLIF(@UsedCarsBaseCurrencyCode, ''), 'USD');
SET @UsedCarsCounterRateToBase = COALESCE(NULLIF(@UsedCarsCounterRateToBase, 0), 1);

UPDATE dbo.UsedCars
SET BaseCurrencyCode = COALESCE(NULLIF(BaseCurrencyCode, ''), @UsedCarsBaseCurrencyCode),
    CounterCurrencyCode = COALESCE(NULLIF(CounterCurrencyCode, ''), @UsedCarsCounterCurrencyCode),
    CounterRateToBase = COALESCE(NULLIF(CounterRateToBase, 0), @UsedCarsCounterRateToBase)
WHERE BaseCurrencyCode IS NULL
   OR CounterCurrencyCode IS NULL
   OR CounterRateToBase IS NULL
   OR CounterRateToBase <= 0;

BEGIN TRY
    ALTER TABLE dbo.UsedCars ALTER COLUMN BaseCurrencyCode CHAR(3) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.UsedCars ALTER COLUMN CounterCurrencyCode CHAR(3) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.UsedCars ALTER COLUMN CounterRateToBase DECIMAL(19, 8) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;");

        conn.Execute(
            @"
IF COL_LENGTH('dbo.UsedCars', 'LocationId') IS NOT NULL
   AND OBJECT_ID('dbo.Location', 'U') IS NOT NULL
BEGIN
    UPDATE uc
    SET LocationId = loc.LocationID
    FROM dbo.UsedCars uc
    INNER JOIN dbo.Location loc
        ON UPPER(LTRIM(RTRIM(loc.Name))) = UPPER(LTRIM(RTRIM(uc.Location)))
    WHERE uc.LocationId IS NULL
      AND NULLIF(LTRIM(RTRIM(uc.Location)), N'') IS NOT NULL;

    UPDATE uc
    SET Location = loc.Name
    FROM dbo.UsedCars uc
    INNER JOIN dbo.Location loc ON loc.LocationID = uc.LocationId
    WHERE uc.LocationId IS NOT NULL
      AND (uc.Location IS NULL OR LTRIM(RTRIM(uc.Location)) = N'');
END;

IF COL_LENGTH('dbo.UsedCars', 'SupplierId') IS NOT NULL
   AND OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL
BEGIN
    UPDATE uc
    SET SupplierId = source.SupplierId
    FROM dbo.UsedCars uc
    OUTER APPLY
    (
        SELECT TOP (1) p.SupplierId
        FROM dbo.UsedCarPurchases p
        WHERE p.UsedCarId = uc.Id
          AND p.SupplierId IS NOT NULL
        ORDER BY p.CreatedAt DESC, p.Id DESC
    ) source
    WHERE uc.SupplierId IS NULL
      AND source.SupplierId IS NOT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'ReceivedAt') IS NOT NULL
BEGIN
    UPDATE dbo.UsedCars
    SET ReceivedAt = NULL
    WHERE ISNULL(IsReceived, 0) = 0
      AND ReceivedAt IS NOT NULL;

    UPDATE dbo.UsedCars
    SET ReceivedAt = COALESCE(ModifiedAt, CreatedAt, SYSUTCDATETIME())
    WHERE ISNULL(IsReceived, 0) = 1
      AND ReceivedAt IS NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_UsedCars_Location'
      AND parent_object_id = OBJECT_ID('dbo.UsedCars'))
   AND OBJECT_ID('dbo.Location', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.UsedCars WITH NOCHECK
        ADD CONSTRAINT FK_UsedCars_Location FOREIGN KEY (LocationId) REFERENCES dbo.Location (LocationId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_UsedCars_Suppliers'
      AND parent_object_id = OBJECT_ID('dbo.UsedCars'))
   AND OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.UsedCars WITH NOCHECK
        ADD CONSTRAINT FK_UsedCars_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCars_LocationId'
      AND object_id = OBJECT_ID('dbo.UsedCars'))
BEGIN
    CREATE INDEX IX_UsedCars_LocationId ON dbo.UsedCars (LocationId);
END;");

        conn.Execute(
            @"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCars_SupplierId'
      AND object_id = OBJECT_ID('dbo.UsedCars'))
BEGIN
    CREATE INDEX IX_UsedCars_SupplierId ON dbo.UsedCars (SupplierId);
END;");
    }
}
