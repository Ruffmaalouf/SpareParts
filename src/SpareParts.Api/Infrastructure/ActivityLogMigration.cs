using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ActivityLogMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.ActivityLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ActivityLogs
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ActivityLogs PRIMARY KEY,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(100) NOT NULL,
        EntityId INT NULL,
        EntityDescription NVARCHAR(500) NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        UserId INT NULL,
        UserName NVARCHAR(200) NULL,
        IpAddress NVARCHAR(50) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ActivityLogs_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.ActivityLogs', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.ActivityLogs') AND name = 'IX_ActivityLogs_EntityType_EntityId')
BEGIN
    CREATE INDEX IX_ActivityLogs_EntityType_EntityId ON dbo.ActivityLogs (EntityType, EntityId);
END;
""");
    }
}
