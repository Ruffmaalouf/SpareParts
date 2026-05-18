using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ReportBuilderAdvancedMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var connection = factory.CreateConnection();
        connection.Execute(
            @"
IF OBJECT_ID('dbo.ReportBuilderSavedReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportBuilderSavedReports
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(160) NOT NULL,
        [Description] NVARCHAR(400) NULL,
        TableKey NVARCHAR(260) NOT NULL,
        DefinitionJson NVARCHAR(MAX) NOT NULL,
        DefaultExportFormat NVARCHAR(16) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_DefaultExportFormat DEFAULT ('xls'),
        PreferredChartType NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_PreferredChartType DEFAULT ('bar'),
        IsSensitive BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_IsSensitive DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_IsActive DEFAULT (1),
        CreatedByUserId INT NOT NULL,
        ModifiedByUserId INT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt DATETIME2(0) NULL
    );
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'Description') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD [Description] NVARCHAR(400) NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'TableKey') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD TableKey NVARCHAR(260) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_TableKey DEFAULT ('dbo.Unknown');
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'DefinitionJson') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD DefinitionJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_DefinitionJson DEFAULT ('{}');
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'DefaultExportFormat') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD DefaultExportFormat NVARCHAR(16) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_DefaultExportFormat_Late DEFAULT ('xls');
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'PreferredChartType') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD PreferredChartType NVARCHAR(20) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_PreferredChartType_Late DEFAULT ('bar');
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'IsSensitive') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD IsSensitive BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_IsSensitive_Late DEFAULT (0);
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'IsActive') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD IsActive BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_IsActive_Late DEFAULT (1);
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD CreatedByUserId INT NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_CreatedByUserId DEFAULT (0);
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'ModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD ModifiedByUserId INT NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderSavedReports_CreatedAt_Late DEFAULT SYSUTCDATETIME();
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'ModifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReports ADD ModifiedAt DATETIME2(0) NULL;
END;

IF OBJECT_ID('dbo.ReportBuilderSavedReportRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportBuilderSavedReportRoles
    (
        ReportId INT NOT NULL,
        RoleName NVARCHAR(100) NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CanView DEFAULT (1),
        CanEdit BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CanEdit DEFAULT (0),
        CanExport BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CanExport DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ReportBuilderSavedReportRoles PRIMARY KEY (ReportId, RoleName),
        CONSTRAINT FK_ReportBuilderSavedReportRoles_Report FOREIGN KEY (ReportId) REFERENCES dbo.ReportBuilderSavedReports(Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.ReportBuilderFavoriteReports', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportBuilderFavoriteReports
    (
        UserId INT NOT NULL,
        ReportId INT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderFavoriteReports_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ReportBuilderFavoriteReports PRIMARY KEY (UserId, ReportId),
        CONSTRAINT FK_ReportBuilderFavoriteReports_Report FOREIGN KEY (ReportId) REFERENCES dbo.ReportBuilderSavedReports(Id) ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.ReportBuilderBackgroundRuns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReportBuilderBackgroundRuns
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ReportName NVARCHAR(160) NOT NULL,
        RequestedByUserId INT NOT NULL,
        RequestedByRoleName NVARCHAR(100) NULL,
        [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_Status DEFAULT ('Queued'),
        ProgressPercent INT NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_Progress DEFAULT (0),
        RequestJson NVARCHAR(MAX) NOT NULL,
        ResultJson NVARCHAR(MAX) NULL,
        [Summary] NVARCHAR(400) NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ResultRowCount INT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_CreatedAt DEFAULT SYSUTCDATETIME(),
        StartedAt DATETIME2(0) NULL,
        CompletedAt DATETIME2(0) NULL
    );
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'RequestedByRoleName') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD RequestedByRoleName NVARCHAR(100) NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD [Status] NVARCHAR(30) NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_Status_Late DEFAULT ('Queued');
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'ProgressPercent') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD ProgressPercent INT NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_Progress_Late DEFAULT (0);
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'RequestJson') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD RequestJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_RequestJson DEFAULT ('{}');
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'ResultJson') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD ResultJson NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'Summary') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD [Summary] NVARCHAR(400) NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'ErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD ErrorMessage NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'ResultRowCount') IS NULL
   AND COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'RowCount') IS NOT NULL
BEGIN
    EXEC sp_rename 'dbo.ReportBuilderBackgroundRuns.[RowCount]', 'ResultRowCount', 'COLUMN';
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'ResultRowCount') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD ResultRowCount INT NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderBackgroundRuns_CreatedAt_Late DEFAULT SYSUTCDATETIME();
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'StartedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD StartedAt DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'CompletedAt') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD CompletedAt DATETIME2(0) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ReportBuilderSavedReports_TableKey'
      AND object_id = OBJECT_ID('dbo.ReportBuilderSavedReports')
)
BEGIN
    CREATE INDEX IX_ReportBuilderSavedReports_TableKey
        ON dbo.ReportBuilderSavedReports (TableKey, IsActive, IsSensitive);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ReportBuilderBackgroundRuns_User'
      AND object_id = OBJECT_ID('dbo.ReportBuilderBackgroundRuns')
)
BEGIN
    CREATE INDEX IX_ReportBuilderBackgroundRuns_User
        ON dbo.ReportBuilderBackgroundRuns (RequestedByUserId, CreatedAt DESC);
END;");
    }
}
