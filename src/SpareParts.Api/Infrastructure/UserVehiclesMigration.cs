using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UserVehiclesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.UserVehicles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserVehicles (
        Id                INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_UserVehicles PRIMARY KEY,
        TenantId          INT            NOT NULL CONSTRAINT DF_UserVehicles_TenantId DEFAULT 0,
        UserId            INT            NOT NULL,
        Nickname          NVARCHAR(100)  NULL,
        VinNumber         NVARCHAR(17)   NULL,
        Make              NVARCHAR(100)  NOT NULL,
        Model             NVARCHAR(100)  NOT NULL,
        Year              INT            NOT NULL,
        Trim              NVARCHAR(100)  NULL,
        EngineCode        NVARCHAR(60)   NULL,
        FuelType          NVARCHAR(60)   NULL,
        Mileage           INT            NULL,
        PhotoUrl          NVARCHAR(500)  NULL,
        IsActive          BIT            NOT NULL CONSTRAINT DF_UserVehicles_IsActive DEFAULT 1,
        IsDefault         BIT            NOT NULL CONSTRAINT DF_UserVehicles_IsDefault DEFAULT 0,
        CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_UserVehicles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId   INT            NULL,
        ModifiedAt        DATETIME2      NULL,
        ModifiedByUserId  INT            NULL
    );
    CREATE INDEX IX_UserVehicles_UserId   ON dbo.UserVehicles (UserId);
    CREATE INDEX IX_UserVehicles_TenantId ON dbo.UserVehicles (TenantId);
END;
""");
    }
}
