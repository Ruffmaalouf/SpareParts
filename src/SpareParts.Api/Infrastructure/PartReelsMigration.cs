using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartReelsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.PartReels', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartReels
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartReels PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_PartReels_TenantId DEFAULT 0,
        PartId INT NULL,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        VideoUrl NVARCHAR(500) NOT NULL,
        ThumbnailUrl NVARCHAR(500) NULL,
        PartName NVARCHAR(200) NULL,
        Price DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_PartReels_Currency DEFAULT 'USD',
        Status INT NOT NULL CONSTRAINT DF_PartReels_Status DEFAULT 1,
        ViewCount INT NOT NULL CONSTRAINT DF_PartReels_ViewCount DEFAULT 0,
        LikeCount INT NOT NULL CONSTRAINT DF_PartReels_LikeCount DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PartReels_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_PartReels_TenantId ON dbo.PartReels (TenantId);
    CREATE INDEX IX_PartReels_Status ON dbo.PartReels (Status);
END;
""");
    }
}
