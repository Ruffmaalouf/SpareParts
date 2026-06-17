using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class MechanicProfilesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.MechanicProfiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.MechanicProfiles
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MechanicProfiles PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_MechanicProfiles_TenantId DEFAULT 0,
        UserId             INT NOT NULL,
        BadgeLevel         TINYINT NOT NULL CONSTRAINT DF_MechanicProfiles_BadgeLevel DEFAULT 0,
        SpecialtyBrands    NVARCHAR(500) NULL,
        SpecialtyTypes     NVARCHAR(500) NULL,
        PurchaseCount      INT NOT NULL CONSTRAINT DF_MechanicProfiles_PurchaseCount DEFAULT 0,
        RepairOrderCount   INT NOT NULL CONSTRAINT DF_MechanicProfiles_RepairOrderCount DEFAULT 0,
        IsVerified         BIT NOT NULL CONSTRAINT DF_MechanicProfiles_IsVerified DEFAULT 0,
        VerifiedAt         DATETIME2 NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_MechanicProfiles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL
    );
    CREATE UNIQUE INDEX UQ_MechanicProfiles_UserId ON dbo.MechanicProfiles (TenantId, UserId);
END;
""");
    }
}
