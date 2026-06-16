using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class RepairOrdersMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.RepairOrders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairOrders
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairOrders PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_RepairOrders_TenantId DEFAULT 0,
        OrderNumber NVARCHAR(40) NOT NULL,
        CustomerName NVARCHAR(120) NOT NULL,
        CustomerPhone NVARCHAR(40) NULL,
        VehicleMake NVARCHAR(100) NULL,
        VehicleModel NVARCHAR(100) NULL,
        VehicleYear INT NULL,
        VinNumber NVARCHAR(17) NULL,
        Notes NVARCHAR(1000) NULL,
        LocationCity NVARCHAR(100) NULL,
        Status INT NOT NULL CONSTRAINT DF_RepairOrders_Status DEFAULT 1,
        SentAt DATETIME2 NULL,
        DeadlineAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_RepairOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_RepairOrders_TenantId ON dbo.RepairOrders (TenantId);
    CREATE INDEX IX_RepairOrders_Status ON dbo.RepairOrders (Status);
END;

IF OBJECT_ID('dbo.RepairOrderItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairOrderItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairOrderItems PRIMARY KEY,
        RepairOrderId INT NOT NULL CONSTRAINT FK_RepairOrderItems_RepairOrders FOREIGN KEY REFERENCES dbo.RepairOrders (Id),
        PartName NVARCHAR(200) NOT NULL,
        OemNumber NVARCHAR(60) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_RepairOrderItems_Quantity DEFAULT 1,
        ConditionPreference NVARCHAR(20) NOT NULL CONSTRAINT DF_RepairOrderItems_ConditionPreference DEFAULT 'Any',
        Notes NVARCHAR(500) NULL,
        MaxBudget DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_RepairOrderItems_Currency DEFAULT 'USD',
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_RepairOrderItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_RepairOrderItems_RepairOrderId ON dbo.RepairOrderItems (RepairOrderId);
END;

IF OBJECT_ID('dbo.RepairOrderQuotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairOrderQuotes
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairOrderQuotes PRIMARY KEY,
        RepairOrderItemId INT NOT NULL CONSTRAINT FK_RepairOrderQuotes_RepairOrderItems FOREIGN KEY REFERENCES dbo.RepairOrderItems (Id),
        TenantId INT NOT NULL CONSTRAINT DF_RepairOrderQuotes_TenantId DEFAULT 0,
        SellerName NVARCHAR(120) NOT NULL,
        SellerPhone NVARCHAR(40) NULL,
        PartId INT NULL,
        OfferedPrice DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_RepairOrderQuotes_Currency DEFAULT 'USD',
        Condition NVARCHAR(60) NOT NULL,
        Notes NVARCHAR(500) NULL,
        IsAccepted BIT NOT NULL CONSTRAINT DF_RepairOrderQuotes_IsAccepted DEFAULT 0,
        ExpiresAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_RepairOrderQuotes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_RepairOrderQuotes_RepairOrderItemId ON dbo.RepairOrderQuotes (RepairOrderItemId);
    CREATE INDEX IX_RepairOrderQuotes_TenantId ON dbo.RepairOrderQuotes (TenantId);
END;
""");
    }
}
