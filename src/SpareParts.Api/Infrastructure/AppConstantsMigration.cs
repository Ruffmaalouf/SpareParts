using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class AppConstantsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.AppConstants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppConstants
    (
        [Key] NVARCHAR(120) NOT NULL PRIMARY KEY,
        [Value] NVARCHAR(MAX) NOT NULL,
        Description NVARCHAR(250) NULL,
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppConstants_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('dbo.AppConstants', 'Value') <> -1
BEGIN
    ALTER TABLE dbo.AppConstants
    ALTER COLUMN [Value] NVARCHAR(MAX) NOT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'BrandRegionOrder')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('BrandRegionOrder', 'German,Japanese,Korean', 'Display order for car brand region groups.');
END;


IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'BaseCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('BaseCurrencyCode', 'USD', 'Application base currency code used for totals and conversions.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'CounterCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('CounterCurrencyCode', 'USD', 'Application counter/default transaction currency code.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DisplayCurrencyCode')
BEGIN
    DECLARE @DisplayCurrencyCode NVARCHAR(3);

    SELECT TOP (1) @DisplayCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode'
      AND LEN(LTRIM(RTRIM([Value]))) = 3;

    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DisplayCurrencyCode', COALESCE(@DisplayCurrencyCode, 'USD'), 'Application display currency code used by screens for money totals.');
END;
IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DefaultCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DefaultCurrencyCode', 'USD', 'Fallback invoice currency code.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DefaultCounterRate')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DefaultCounterRate', '1', 'Fallback counter/base rate when no currency mapping exists.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DefaultSalesTransactionTypeName')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DefaultSalesTransactionTypeName', 'Sales', 'Default transaction type used when creating invoices.');
END;");
    }
}
