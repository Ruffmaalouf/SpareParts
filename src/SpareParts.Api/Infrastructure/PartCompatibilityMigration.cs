using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartCompatibilityMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.PartCompatibilities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartCompatibilities
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartCompatibilities PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_PartCompatibilities_TenantId DEFAULT 0,
        PartId INT NOT NULL,
        CompatibleMakes NVARCHAR(500) NULL,
        CompatibleModels NVARCHAR(500) NULL,
        YearFrom INT NULL,
        YearTo INT NULL,
        CompatibleEngines NVARCHAR(200) NULL,
        OemNumbers NVARCHAR(500) NULL,
        AlternativeNumbers NVARCHAR(500) NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PartCompatibilities_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT UQ_PartCompatibilities_PartId UNIQUE (PartId)
    );
    CREATE INDEX IX_PartCompatibilities_TenantId ON dbo.PartCompatibilities (TenantId);
END;
""");
    }
}
