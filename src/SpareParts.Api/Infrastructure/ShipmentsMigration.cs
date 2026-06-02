using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ShipmentsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.Shipments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Shipments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shipments PRIMARY KEY,
        ShipmentNumber NVARCHAR(50) NOT NULL CONSTRAINT DF_Shipments_ShipmentNumber DEFAULT N'',
        SalesInvoiceId INT NULL,
        CustomerId INT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Shipments_Status DEFAULT N'Pending',
        CarrierName NVARCHAR(200) NULL,
        TrackingNumber NVARCHAR(100) NULL,
        DeliveryAddress NVARCHAR(500) NULL,
        EstimatedDelivery DATETIME2(0) NULL,
        ActualDelivery DATETIME2(0) NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Shipments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Shipments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;

IF OBJECT_ID('dbo.ShipmentEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShipmentEvents
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShipmentEvents PRIMARY KEY,
        ShipmentId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Location NVARCHAR(200) NULL,
        Notes NVARCHAR(1000) NULL,
        EventAt DATETIME2(0) NOT NULL CONSTRAINT DF_ShipmentEvents_EventAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_ShipmentEvents_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (Id)
    );
END;
""");
    }
}
