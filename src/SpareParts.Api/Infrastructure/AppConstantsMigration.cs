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
        [Value] NVARCHAR(4000) NOT NULL,
        Description NVARCHAR(250) NULL,
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppConstants_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'BrandRegionOrder')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('BrandRegionOrder', 'German,Japanese,Korean', 'Display order for car brand region groups.');
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
