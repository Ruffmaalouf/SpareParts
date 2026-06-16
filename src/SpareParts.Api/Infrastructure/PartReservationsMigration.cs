using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartReservationsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.PartReservations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartReservations
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartReservations PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_PartReservations_TenantId DEFAULT 0,
        PartId INT NOT NULL,
        ReservedByUserId INT NULL,
        ReservedByName NVARCHAR(120) NOT NULL,
        ReservedByPhone NVARCHAR(40) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_PartReservations_Quantity DEFAULT 1,
        HoldFee DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_PartReservations_Currency DEFAULT 'USD',
        Status INT NOT NULL CONSTRAINT DF_PartReservations_Status DEFAULT 1,
        ExpiresAt DATETIME2 NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PartReservations_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_PartReservations_PartId ON dbo.PartReservations (PartId);
    CREATE INDEX IX_PartReservations_TenantId ON dbo.PartReservations (TenantId);
    CREATE INDEX IX_PartReservations_Status ON dbo.PartReservations (Status);
END;
""");
    }
}
