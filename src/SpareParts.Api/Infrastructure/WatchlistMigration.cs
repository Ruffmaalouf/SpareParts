using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class WatchlistMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.WatchedParts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WatchedParts (
        Id                INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_WatchedParts PRIMARY KEY,
        TenantId          INT            NOT NULL CONSTRAINT DF_WatchedParts_TenantId DEFAULT 0,
        UserId            INT            NOT NULL,
        PartName          NVARCHAR(200)  NULL,
        VehicleMake       NVARCHAR(100)  NULL,
        VehicleModel      NVARCHAR(100)  NULL,
        VehicleYear       INT            NULL,
        VinNumber         NVARCHAR(17)   NULL,
        MaxBudget         DECIMAL(18,2)  NULL,
        Currency          NVARCHAR(10)   NOT NULL CONSTRAINT DF_WatchedParts_Currency DEFAULT N'USD',
        Notes             NVARCHAR(500)  NULL,
        IsActive          BIT            NOT NULL CONSTRAINT DF_WatchedParts_IsActive DEFAULT 1,
        LastAlertedAt     DATETIME2      NULL,
        CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_WatchedParts_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId   INT            NULL,
        ModifiedAt        DATETIME2      NULL,
        ModifiedByUserId  INT            NULL
    );
    CREATE INDEX IX_WatchedParts_UserId   ON dbo.WatchedParts (UserId);
    CREATE INDEX IX_WatchedParts_TenantId ON dbo.WatchedParts (TenantId);
END;
""");
    }
}
