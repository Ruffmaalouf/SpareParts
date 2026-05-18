using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class BarcodeScanningMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Parts', 'Barcode') IS NULL
            BEGIN
                ALTER TABLE dbo.Parts ADD Barcode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Warehouses', 'Barcode') IS NULL
            BEGIN
                ALTER TABLE dbo.Warehouses ADD Barcode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.UsedCars', 'Barcode') IS NULL
            BEGIN
                ALTER TABLE dbo.UsedCars ADD Barcode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.StockMovements', 'ScanCode') IS NULL
            BEGIN
                ALTER TABLE dbo.StockMovements ADD ScanCode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Transactions', 'ScanCode') IS NULL
            BEGIN
                ALTER TABLE dbo.Transactions ADD ScanCode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Parts', 'Barcode') IS NOT NULL
            BEGIN
                UPDATE dbo.Parts
                SET Barcode = NULLIF(LTRIM(RTRIM(InternalCode)), N'')
                WHERE (Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'')
                  AND NULLIF(LTRIM(RTRIM(InternalCode)), N'') IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Warehouses', 'Barcode') IS NOT NULL
            BEGIN
                UPDATE dbo.Warehouses
                SET Barcode = CONCAT(N'WH-', Id)
                WHERE Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'';
            END;

            IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.UsedCars', 'Barcode') IS NOT NULL
            BEGIN
                UPDATE dbo.UsedCars
                SET Barcode = CONCAT(N'UC-', Id)
                WHERE Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'';
            END;

            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.StockMovements', 'ScanCode') IS NOT NULL
            BEGIN
                UPDATE dbo.StockMovements
                SET ScanCode = CONCAT(N'SM-', Id)
                WHERE ScanCode IS NULL OR LTRIM(RTRIM(ScanCode)) = N'';
            END;

            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Transactions', 'ScanCode') IS NOT NULL
            BEGIN
                UPDATE dbo.Transactions
                SET ScanCode = NULLIF(LTRIM(RTRIM(TransactionNumber)), N'')
                WHERE (ScanCode IS NULL OR LTRIM(RTRIM(ScanCode)) = N'')
                  AND NULLIF(LTRIM(RTRIM(TransactionNumber)), N'') IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Parts') AND name = 'IX_Parts_Barcode')
            BEGIN
                CREATE INDEX IX_Parts_Barcode ON dbo.Parts(Barcode) WHERE Barcode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Warehouses') AND name = 'IX_Warehouses_Barcode')
            BEGIN
                CREATE INDEX IX_Warehouses_Barcode ON dbo.Warehouses(Barcode) WHERE Barcode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.UsedCars') AND name = 'IX_UsedCars_Barcode')
            BEGIN
                CREATE INDEX IX_UsedCars_Barcode ON dbo.UsedCars(Barcode) WHERE Barcode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.StockMovements') AND name = 'IX_StockMovements_ScanCode')
            BEGIN
                CREATE INDEX IX_StockMovements_ScanCode ON dbo.StockMovements(ScanCode) WHERE ScanCode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Transactions') AND name = 'IX_Transactions_ScanCode')
            BEGIN
                CREATE INDEX IX_Transactions_ScanCode ON dbo.Transactions(ScanCode) WHERE ScanCode IS NOT NULL;
            END;
            """);
    }
}
