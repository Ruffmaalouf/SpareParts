using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ReportBuilderLinksMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var connection = factory.CreateConnection();
        connection.Execute(
            @"
IF OBJECT_ID('dbo.ReportBuilderTableLinks', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportBuilderTableLinks
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        LinkName NVARCHAR(160) NOT NULL,
        SourceTableKey NVARCHAR(260) NOT NULL,
        SourceColumnName NVARCHAR(128) NOT NULL,
        TargetTableKey NVARCHAR(260) NOT NULL,
        TargetColumnName NVARCHAR(128) NOT NULL,
        JoinType NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportBuilderTableLinks_JoinType DEFAULT ('LEFT'),
        IsActive BIT NOT NULL CONSTRAINT DF_ReportBuilderTableLinks_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderTableLinks_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt DATETIME2(0) NULL
    );
END;

IF COL_LENGTH('dbo.ReportBuilderTableLinks', 'JoinType') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderTableLinks
    ADD JoinType NVARCHAR(20) NOT NULL
        CONSTRAINT DF_ReportBuilderTableLinks_JoinType_Late DEFAULT ('LEFT');
END;

IF COL_LENGTH('dbo.ReportBuilderTableLinks', 'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderTableLinks
    ADD IsActive BIT NOT NULL
        CONSTRAINT DF_ReportBuilderTableLinks_IsActive_Late DEFAULT (1);
END;

IF COL_LENGTH('dbo.ReportBuilderTableLinks', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderTableLinks
    ADD CreatedAt DATETIME2(0) NOT NULL
        CONSTRAINT DF_ReportBuilderTableLinks_CreatedAt_Late DEFAULT SYSUTCDATETIME();
END;

IF COL_LENGTH('dbo.ReportBuilderTableLinks', 'ModifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderTableLinks
    ADD ModifiedAt DATETIME2(0) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ReportBuilderTableLinks_Definition'
      AND object_id = OBJECT_ID('dbo.ReportBuilderTableLinks')
)
BEGIN
    CREATE UNIQUE INDEX UX_ReportBuilderTableLinks_Definition
        ON dbo.ReportBuilderTableLinks (SourceTableKey, SourceColumnName, TargetTableKey, TargetColumnName);
END;

UPDATE dbo.ReportBuilderTableLinks
SET JoinType = UPPER(LTRIM(RTRIM(ISNULL(JoinType, 'LEFT'))))
WHERE JoinType IS NULL
   OR JoinType <> UPPER(LTRIM(RTRIM(ISNULL(JoinType, 'LEFT'))));");
    }
}
