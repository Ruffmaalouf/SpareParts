using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class GarageStockMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.GarageStockItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.GarageStockItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_GarageStockItems PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_GarageStockItems_TenantId DEFAULT 0,
        ItemName NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NULL,
        Barcode NVARCHAR(60) NULL,
        OemNumber NVARCHAR(60) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_GarageStockItems_Quantity DEFAULT 0,
        MinimumQuantity INT NOT NULL CONSTRAINT DF_GarageStockItems_MinimumQuantity DEFAULT 0,
        PreferredSupplierId NVARCHAR(60) NULL,
        Notes NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_GarageStockItems_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_GarageStockItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_GarageStockItems_TenantId ON dbo.GarageStockItems (TenantId);
END;
""");
    }
}
