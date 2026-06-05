using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class TenantsMigration
{
    public const int DefaultTenantId = 1;
    public const string DefaultTenantCode = "default";

    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.Tenants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants (
        Id        INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Name      NVARCHAR(160) NOT NULL,
        Code      NVARCHAR(60)  NOT NULL,
        Domain    NVARCHAR(255) NULL,
        IsActive  BIT           NOT NULL DEFAULT 1,
        CreatedAt DATETIME2     NOT NULL CONSTRAINT DF_Tenants_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2     NULL,
        CONSTRAINT UQ_Tenants_Code UNIQUE (Code)
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Id = 1)
BEGIN
    SET IDENTITY_INSERT dbo.Tenants ON;
    INSERT INTO dbo.Tenants (Id, Name, Code, IsActive, CreatedAt)
    VALUES (1, N'Default', N'default', 1, SYSUTCDATETIME());
    SET IDENTITY_INSERT dbo.Tenants OFF;
END;

IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 5)
    BEGIN
        SET IDENTITY_INSERT dbo.Roles ON;
        INSERT INTO dbo.Roles (Id, Name, Description, IsSystem, IsActive, CreatedAt)
        VALUES (5, N'Super Admin', N'Platform-level super administrator', 1, 1, SYSUTCDATETIME());
        SET IDENTITY_INSERT dbo.Roles OFF;
    END;
END;
""");
    }
}
