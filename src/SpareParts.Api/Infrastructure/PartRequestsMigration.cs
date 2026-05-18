using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartRequestsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.PartRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartRequests
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartRequests PRIMARY KEY,
        PartId INT NULL,
        CustomerId INT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        CustomerPhone NVARCHAR(80) NULL,
        RequestedPartName NVARCHAR(240) NOT NULL,
        RequestedOemNumber NVARCHAR(120) NULL,
        VehicleDetails NVARCHAR(240) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_PartRequests_Quantity DEFAULT 1,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_PartRequests_Status DEFAULT N'Open',
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        ClosedAt DATETIME2(0) NULL,
        ClosedByUserId INT NULL,
        CONSTRAINT FK_PartRequests_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_PartRequests_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_PartRequests_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_PartRequests_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_PartRequests_ClosedByUsers FOREIGN KEY (ClosedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_PartRequests_Quantity_Positive CHECK (Quantity > 0),
        CONSTRAINT CK_PartRequests_Status CHECK (Status IN (N'Open', N'Contacted', N'Fulfilled', N'Cancelled'))
    );
END;

IF OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PartRequests') AND name = 'IX_PartRequests_Status_CreatedAt')
BEGIN
    CREATE INDEX IX_PartRequests_Status_CreatedAt ON dbo.PartRequests (Status, CreatedAt DESC);
END;

IF OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PartRequests') AND name = 'IX_PartRequests_PartId')
BEGIN
    CREATE INDEX IX_PartRequests_PartId ON dbo.PartRequests (PartId) WHERE PartId IS NOT NULL;
END;
""");
    }
}
