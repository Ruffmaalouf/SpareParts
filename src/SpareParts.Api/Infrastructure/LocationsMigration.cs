using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class LocationsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
DECLARE @DefaultLocationCurrencyCode CHAR(3) = NULL;

IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    SELECT TOP (1) @DefaultLocationCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode'
      AND LEN(LTRIM(RTRIM([Value]))) = 3;

    IF @DefaultLocationCurrencyCode IS NULL
    BEGIN
        SELECT TOP (1) @DefaultLocationCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
        FROM dbo.AppConstants
        WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode')
          AND LEN(LTRIM(RTRIM([Value]))) = 3
        ORDER BY CASE WHEN [Key] = 'BaseCurrencyCode' THEN 0 ELSE 1 END;
    END;
END;

IF @DefaultLocationCurrencyCode IS NULL
BEGIN
    SET @DefaultLocationCurrencyCode = 'USD';
END;

IF OBJECT_ID('dbo.Location', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Location
    (
        LocationID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Location PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        ShippingFees DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Location_ShippingFees DEFAULT (0),
        ShippingFeesCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_Location_ShippingFeesCurrencyCode DEFAULT ('USD'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Location_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Location_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id)
    );
END;

IF COL_LENGTH('dbo.Location', 'Name') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD Name NVARCHAR(160) NULL;

    IF COL_LENGTH('dbo.Location', 'Description') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET Name = NULLIF(LTRIM(RTRIM(CAST([Description] AS NVARCHAR(160)))), N'''')
              WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'''';');
    END;

    IF COL_LENGTH('dbo.Location', 'Code') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET Name = NULLIF(LTRIM(RTRIM(CAST([Code] AS NVARCHAR(160)))), N'''')
              WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'''';');
    END;

    UPDATE dbo.Location
    SET Name = N'Location ' + CAST(LocationID AS NVARCHAR(20))
    WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'';

    ALTER TABLE dbo.Location ALTER COLUMN Name NVARCHAR(160) NOT NULL;
END;

IF COL_LENGTH('dbo.Location', 'ShippingFees') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ShippingFees DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Location_ShippingFees DEFAULT (0);

    IF COL_LENGTH('dbo.Location', 'Shipping') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET ShippingFees = ISNULL(TRY_CONVERT(DECIMAL(18, 2), [Shipping]), 0);');
    END;
END;

IF COL_LENGTH('dbo.Location', 'ShippingFeesCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ShippingFeesCurrencyCode CHAR(3) NULL;

    IF COL_LENGTH('dbo.Location', 'ShippingCurrencyCode') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET ShippingFeesCurrencyCode = UPPER(LEFT(LTRIM(RTRIM(CAST([ShippingCurrencyCode] AS NVARCHAR(16)))), 3))
              WHERE ShippingFeesCurrencyCode IS NULL
                 OR LTRIM(RTRIM(ShippingFeesCurrencyCode)) = N'''';');
    END;

    IF COL_LENGTH('dbo.Location', 'CurrencyCode') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET ShippingFeesCurrencyCode = UPPER(LEFT(LTRIM(RTRIM(CAST([CurrencyCode] AS NVARCHAR(16)))), 3))
              WHERE ShippingFeesCurrencyCode IS NULL
                 OR LTRIM(RTRIM(ShippingFeesCurrencyCode)) = N'''';');
    END;
END;

UPDATE dbo.Location
SET ShippingFeesCurrencyCode = @DefaultLocationCurrencyCode
WHERE ShippingFeesCurrencyCode IS NULL
   OR LEN(LTRIM(RTRIM(ShippingFeesCurrencyCode))) <> 3;

IF COL_LENGTH('dbo.Location', 'ShippingFeesCurrencyCode') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Location ALTER COLUMN ShippingFeesCurrencyCode CHAR(3) NOT NULL;
END;

IF COL_LENGTH('dbo.Location', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSUTCDATETIME();
END;

IF COL_LENGTH('dbo.Location', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD CreatedByUserId INT NULL;
    ALTER TABLE dbo.Location WITH NOCHECK
        ADD CONSTRAINT FK_Location_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id);
END;

IF COL_LENGTH('dbo.Location', 'ModifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ModifiedAt DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.Location', 'ModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ModifiedByUserId INT NULL;
    ALTER TABLE dbo.Location WITH NOCHECK
        ADD CONSTRAINT FK_Location_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Location_Name'
      AND object_id = OBJECT_ID('dbo.Location'))
BEGIN
    CREATE INDEX IX_Location_Name ON dbo.Location (Name);
END;");
    }
}
