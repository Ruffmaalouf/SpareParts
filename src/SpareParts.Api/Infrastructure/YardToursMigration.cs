using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class YardToursMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.YardTours', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.YardTours
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_YardTours PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_YardTours_TenantId DEFAULT 0,
        Title              NVARCHAR(200) NOT NULL,
        Description        NVARCHAR(1000) NULL,
        ScheduledAt        DATETIME2 NULL,
        StreamUrl          NVARCHAR(500) NULL,
        Status             TINYINT NOT NULL CONSTRAINT DF_YardTours_Status DEFAULT 1,
        ViewerCount        INT NOT NULL CONSTRAINT DF_YardTours_ViewerCount DEFAULT 0,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_YardTours_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL
    );
END;
IF OBJECT_ID('dbo.YardTourInterests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.YardTourInterests
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_YardTourInterests PRIMARY KEY,
        YardTourId         INT NOT NULL,
        Name               NVARCHAR(200) NOT NULL,
        Phone              NVARCHAR(50) NULL,
        PartRequests       NVARCHAR(1000) NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_YardTourInterests_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_YardTourInterests_YardTourId ON dbo.YardTourInterests (YardTourId);
END;
""");
    }
}
