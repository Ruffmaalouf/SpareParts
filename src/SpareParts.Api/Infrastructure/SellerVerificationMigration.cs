using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class SellerVerificationMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.SellerVerifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SellerVerifications (
        Id                     INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_SellerVerifications PRIMARY KEY,
        TenantId               INT            NOT NULL CONSTRAINT UQ_SellerVerifications_TenantId UNIQUE,
        Status                 INT            NOT NULL CONSTRAINT DF_SellerVerifications_Status DEFAULT 0,
        BusinessName           NVARCHAR(200)  NULL,
        RegistrationNumber     NVARCHAR(100)  NULL,
        PhoneVerified          BIT            NOT NULL CONSTRAINT DF_SellerVerifications_PhoneVerified DEFAULT 0,
        IdDocumentUrl          NVARCHAR(500)  NULL,
        BusinessDocumentUrl    NVARCHAR(500)  NULL,
        PhysicalAddress        NVARCHAR(500)  NULL,
        AdminNotes             NVARCHAR(1000) NULL,
        VerifiedAt             DATETIME2      NULL,
        SubmittedAt            DATETIME2      NULL,
        CreatedAt              DATETIME2      NOT NULL CONSTRAINT DF_SellerVerifications_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId        INT            NULL,
        ModifiedAt             DATETIME2      NULL,
        ModifiedByUserId       INT            NULL
    );
END;

IF OBJECT_ID('dbo.Tenants', 'U') IS NOT NULL AND OBJECT_ID('dbo.SellerVerifications', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.SellerVerifications (TenantId, Status, CreatedAt)
    SELECT t.Id, 0, SYSUTCDATETIME()
    FROM dbo.Tenants t
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.SellerVerifications sv WHERE sv.TenantId = t.Id
    );
END;
""");
    }
}
