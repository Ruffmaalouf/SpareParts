using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class SupplierPriceHistoryMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.SupplierPriceHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierPriceHistory
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPriceHistory PRIMARY KEY,
        PartId INT NOT NULL,
        SupplierId INT NOT NULL,
        UnitPrice DECIMAL(19,4) NOT NULL,
        CurrencyCode NVARCHAR(3) NULL CONSTRAINT DF_SupplierPriceHistory_CurrencyCode DEFAULT N'USD',
        Quantity INT NOT NULL CONSTRAINT DF_SupplierPriceHistory_Quantity DEFAULT 1,
        InvoiceId INT NULL,
        RecordedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SupplierPriceHistory_RecordedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_SupplierPriceHistory_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_SupplierPriceHistory_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id)
    );
END;

IF OBJECT_ID('dbo.SupplierPriceHistory', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.SupplierPriceHistory') AND name = 'IX_SupplierPriceHistory_PartId')
BEGIN
    CREATE INDEX IX_SupplierPriceHistory_PartId ON dbo.SupplierPriceHistory (PartId, SupplierId, RecordedAt DESC);
END;
""");
    }
}
