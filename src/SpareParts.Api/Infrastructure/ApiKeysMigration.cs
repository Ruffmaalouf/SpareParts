using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ApiKeysMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.ApiKeys', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApiKeys
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApiKeys PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_ApiKeys_TenantId DEFAULT 0,
        Name               NVARCHAR(200) NOT NULL,
        KeyHash            NVARCHAR(128) NOT NULL,
        KeyPrefix          NVARCHAR(20) NOT NULL,
        Scopes             NVARCHAR(500) NULL,
        IsActive           BIT NOT NULL CONSTRAINT DF_ApiKeys_IsActive DEFAULT 1,
        ExpiresAt          DATETIME2 NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_ApiKeys_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL,
        CONSTRAINT UQ_ApiKeys_KeyHash UNIQUE (KeyHash)
    );
    CREATE INDEX IX_ApiKeys_TenantId ON dbo.ApiKeys (TenantId);
END;
""");
    }
}
