using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ContentReportsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.ContentReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContentReports
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContentReports PRIMARY KEY,
        TenantId            INT NOT NULL CONSTRAINT DF_ContentReports_TenantId DEFAULT 0,
        TargetType          TINYINT NOT NULL,
        TargetId            INT NOT NULL,
        ReportedByUserId    INT NOT NULL,
        Category            TINYINT NOT NULL,
        Details             NVARCHAR(1000) NULL,
        Status              TINYINT NOT NULL CONSTRAINT DF_ContentReports_Status DEFAULT 1,
        AutoFlagged         BIT NOT NULL CONSTRAINT DF_ContentReports_AutoFlagged DEFAULT 0,
        CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_ContentReports_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt          DATETIME2 NULL
    );
    CREATE INDEX IX_ContentReports_TargetId ON dbo.ContentReports (TargetType, TargetId);
END;
""");
    }
}
