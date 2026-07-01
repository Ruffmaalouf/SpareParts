-- ============================================================
-- Run FIRST — creates dbo.PartImageEnrichment if it doesn't exist
-- Then run run_this_in_ssms.sql
-- ============================================================
USE SparePartsDb;
GO

-- Rollback any open transaction from a previous failed run
IF @@TRANCOUNT > 0
BEGIN
    ROLLBACK TRANSACTION;
    PRINT 'Rolled back an open transaction.';
END
GO

IF OBJECT_ID('dbo.PartImageEnrichment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartImageEnrichment
    (
        Id                  INT            IDENTITY(1,1) NOT NULL
                                            CONSTRAINT PK_PartImageEnrichment PRIMARY KEY,
        PartId              INT            NOT NULL,
        SelectedImageUrl    NVARCHAR(1000) NULL,
        ThumbUrl            NVARCHAR(1000) NULL,
        SourcePageUrl       NVARCHAR(1000) NULL,
        SourceDomain        NVARCHAR(200)  NULL,
        SearchQueryUsed     NVARCHAR(500)  NULL,
        ConfidenceScore     DECIMAL(5,2)   NOT NULL CONSTRAINT DF_PIE_ConfidenceScore  DEFAULT 0,
        ConfidenceLevel     NVARCHAR(20)   NOT NULL CONSTRAINT DF_PIE_ConfidenceLevel  DEFAULT 'low',
        MatchReason         NVARCHAR(500)  NULL,
        ContentType         NVARCHAR(100)  NULL,
        ContentLengthBytes  INT            NULL,
        ImageReachable      BIT            NULL,
        OldImageUrl         NVARCHAR(1000) NULL,
        Status              NVARCHAR(50)   NOT NULL CONSTRAINT DF_PIE_Status           DEFAULT 'pending',
        FetchedAt           DATETIME2      NOT NULL CONSTRAINT DF_PIE_FetchedAt        DEFAULT SYSUTCDATETIME(),
        ApprovedAt          DATETIME2      NULL,
        ApprovedByUserId    INT            NULL,
        AppliedAt           DATETIME2      NULL,
        ErrorMessage        NVARCHAR(1000) NULL,
        CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_PIE_CreatedAt        DEFAULT SYSUTCDATETIME(),
        ModifiedAt          DATETIME2      NULL,
        CONSTRAINT FK_PartImageEnrichment_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartImageEnrichment_PartId          ON dbo.PartImageEnrichment (PartId);
    CREATE INDEX IX_PartImageEnrichment_Status          ON dbo.PartImageEnrichment (Status);
    CREATE INDEX IX_PartImageEnrichment_ConfidenceLevel ON dbo.PartImageEnrichment (ConfidenceLevel);
    PRINT 'Created dbo.PartImageEnrichment table and indexes.';
END
ELSE
BEGIN
    PRINT 'dbo.PartImageEnrichment already exists — skipped.';
END
GO

-- Idempotent column additions (matches PartImageEnrichmentMigration.cs)
IF COL_LENGTH('dbo.PartImageEnrichment', 'ThumbUrl')           IS NULL ALTER TABLE dbo.PartImageEnrichment ADD ThumbUrl           NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'ContentType')        IS NULL ALTER TABLE dbo.PartImageEnrichment ADD ContentType        NVARCHAR(100)  NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'ContentLengthBytes') IS NULL ALTER TABLE dbo.PartImageEnrichment ADD ContentLengthBytes INT            NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'ImageReachable')     IS NULL ALTER TABLE dbo.PartImageEnrichment ADD ImageReachable     BIT            NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'AppliedAt')          IS NULL ALTER TABLE dbo.PartImageEnrichment ADD AppliedAt          DATETIME2      NULL;
GO

PRINT 'Setup complete. Run run_this_in_ssms.sql next.';
GO
