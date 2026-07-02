-- ── PartImageEnrichment Migration ──────────────────────────────────────────────
-- Creates the part_image_enrichment table used by the enrichment script.
-- Safe to run multiple times (idempotent).
-- ────────────────────────────────────────────────────────────────────────────────

IF OBJECT_ID('dbo.PartImageEnrichment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartImageEnrichment
    (
        Id                  INT            IDENTITY(1,1) NOT NULL
                                            CONSTRAINT PK_PartImageEnrichment PRIMARY KEY,

        -- The part being enriched
        PartId              INT            NOT NULL,

        -- The proposed new image
        SelectedImageUrl    NVARCHAR(1000) NULL,
        ThumbUrl            NVARCHAR(1000) NULL,

        -- Where we found it
        SourcePageUrl       NVARCHAR(1000) NULL,
        SourceDomain        NVARCHAR(200)  NULL,
        SearchQueryUsed     NVARCHAR(500)  NULL,

        -- Scoring
        ConfidenceScore     DECIMAL(5,2)   NOT NULL DEFAULT 0,
        ConfidenceLevel     NVARCHAR(20)   NOT NULL DEFAULT 'low',   -- high / medium / low
        MatchReason         NVARCHAR(500)  NULL,

        -- Image validation metadata
        ContentType         NVARCHAR(100)  NULL,
        ContentLengthBytes  INT            NULL,
        ImageReachable      BIT            NULL,

        -- The old image URL (for rollback reference)
        OldImageUrl         NVARCHAR(1000) NULL,
        CurrentImageStatus  NVARCHAR(40)   NULL,

        -- Workflow status
        Status              NVARCHAR(50)   NOT NULL DEFAULT 'pending',
        -- Values: pending / auto_approved / pending_review / skipped / failed / rolled_back
        Provider            NVARCHAR(60)   NULL,
        License             NVARCHAR(120)  NULL,
        ApprovedByAdmin     BIT            NOT NULL
                                            CONSTRAINT DF_PartImageEnrichment_ApprovedByAdmin DEFAULT 0,

        FetchedAt           DATETIME2      NOT NULL
                                            CONSTRAINT DF_PartImageEnrichment_FetchedAt DEFAULT SYSUTCDATETIME(),
        ApprovedAt          DATETIME2      NULL,
        ApprovedByUserId    INT            NULL,
        AppliedAt           DATETIME2      NULL,

        ErrorMessage        NVARCHAR(1000) NULL,

        -- Audit
        CreatedAt           DATETIME2      NOT NULL
                                            CONSTRAINT DF_PartImageEnrichment_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt          DATETIME2      NULL,

        CONSTRAINT FK_PartImageEnrichment_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );

    CREATE INDEX IX_PartImageEnrichment_PartId
        ON dbo.PartImageEnrichment (PartId);

    CREATE INDEX IX_PartImageEnrichment_Status
        ON dbo.PartImageEnrichment (Status);

    CREATE INDEX IX_PartImageEnrichment_ConfidenceLevel
        ON dbo.PartImageEnrichment (ConfidenceLevel);

    PRINT 'PartImageEnrichment table created.';
END
ELSE
BEGIN
    PRINT 'PartImageEnrichment table already exists — skipped.';
END;
GO

-- Add any missing columns if the table was created with an older version of this script
IF COL_LENGTH('dbo.PartImageEnrichment', 'ThumbUrl') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD ThumbUrl NVARCHAR(1000) NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'ContentType') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD ContentType NVARCHAR(100) NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'ContentLengthBytes') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD ContentLengthBytes INT NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'ImageReachable') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD ImageReachable BIT NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'AppliedAt') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD AppliedAt DATETIME2 NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'CurrentImageStatus') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD CurrentImageStatus NVARCHAR(40) NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'Provider') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD Provider NVARCHAR(60) NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'License') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD License NVARCHAR(120) NULL;
GO
IF COL_LENGTH('dbo.PartImageEnrichment', 'ApprovedByAdmin') IS NULL
    ALTER TABLE dbo.PartImageEnrichment ADD ApprovedByAdmin BIT NOT NULL
        CONSTRAINT DF_PartImageEnrichment_ApprovedByAdmin DEFAULT 0;
GO

IF OBJECT_ID('dbo.PartImageEnrichmentCandidates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartImageEnrichmentCandidates
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartImageEnrichmentCandidates PRIMARY KEY,
        PartId              INT NOT NULL,
        CandidateRank       INT NOT NULL,
        ImageUrl            NVARCHAR(1000) NOT NULL,
        ThumbUrl            NVARCHAR(1000) NULL,
        SourcePageUrl       NVARCHAR(1000) NULL,
        SourceDomain        NVARCHAR(200) NULL,
        SearchQueryUsed     NVARCHAR(500) NULL,
        QueryTier           NVARCHAR(40) NULL,
        Provider            NVARCHAR(60) NULL,
        License             NVARCHAR(120) NULL,
        LicenseKnown        BIT NOT NULL CONSTRAINT DF_PIEC_LicenseKnown DEFAULT 0,
        ConfidenceScore     DECIMAL(5,2) NOT NULL CONSTRAINT DF_PIEC_ConfidenceScore DEFAULT 0,
        ConfidenceLevel     NVARCHAR(20) NOT NULL CONSTRAINT DF_PIEC_ConfidenceLevel DEFAULT 'low',
        MatchReason         NVARCHAR(1000) NULL,
        ImageReachable      BIT NULL,
        ContentType         NVARCHAR(100) NULL,
        ContentLengthBytes  BIGINT NULL,
        RejectionReason     NVARCHAR(200) NULL,
        Status              NVARCHAR(50) NOT NULL CONSTRAINT DF_PIEC_Status DEFAULT 'pending',
        FetchedAt           DATETIME2 NOT NULL CONSTRAINT DF_PIEC_FetchedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_PIEC_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PartImageEnrichmentCandidates_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartImageEnrichmentCandidates_PartId ON dbo.PartImageEnrichmentCandidates (PartId);
END;
GO

IF OBJECT_ID('dbo.PartOemEnrichment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartOemEnrichment
    (
        Id                         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartOemEnrichment PRIMARY KEY,
        PartId                     INT NOT NULL,
        ExistingOemNumber          NVARCHAR(200) NULL,
        ProposedPrimaryOemNumber   NVARCHAR(200) NULL,
        AlternativeOemNumbers      NVARCHAR(1000) NULL,
        ManufacturerPartNumber     NVARCHAR(200) NULL,
        SupplierPartNumber         NVARCHAR(200) NULL,
        SourceUrl                  NVARCHAR(1000) NULL,
        SourceDomain               NVARCHAR(200) NULL,
        SearchQueryUsed            NVARCHAR(500) NULL,
        SearchQueriesExecuted      INT NOT NULL CONSTRAINT DF_POE_SearchQueriesExecuted DEFAULT 0,
        TextResultsFound           INT NOT NULL CONSTRAINT DF_POE_TextResultsFound DEFAULT 0,
        ConfidenceScore            DECIMAL(5,2) NOT NULL CONSTRAINT DF_POE_ConfidenceScore DEFAULT 0,
        ConfidenceLevel            NVARCHAR(20) NOT NULL CONSTRAINT DF_POE_ConfidenceLevel DEFAULT 'low',
        MatchReason                NVARCHAR(1000) NULL,
        Status                     NVARCHAR(50) NOT NULL CONSTRAINT DF_POE_Status DEFAULT 'pending_review',
        FetchedAt                  DATETIME2 NOT NULL CONSTRAINT DF_POE_FetchedAt DEFAULT SYSUTCDATETIME(),
        ApprovedByAdmin            BIT NOT NULL CONSTRAINT DF_POE_ApprovedByAdmin DEFAULT 0,
        ErrorMessage               NVARCHAR(1000) NULL,
        CreatedAt                  DATETIME2 NOT NULL CONSTRAINT DF_POE_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt                 DATETIME2 NULL,
        CONSTRAINT FK_PartOemEnrichment_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartOemEnrichment_PartId ON dbo.PartOemEnrichment (PartId);
    CREATE INDEX IX_PartOemEnrichment_Status ON dbo.PartOemEnrichment (Status);
END;
GO

IF OBJECT_ID('dbo.VehicleExpectedPartCandidates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleExpectedPartCandidates
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleExpectedPartCandidates PRIMARY KEY,
        DonorVehicleId       INT NOT NULL,
        ExpectedPartName     NVARCHAR(300) NOT NULL,
        Category             NVARCHAR(200) NULL,
        ExpectedOemNumbers   NVARCHAR(1000) NULL,
        CompatibleYearRange  NVARCHAR(100) NULL,
        Engine               NVARCHAR(200) NULL,
        BodyType             NVARCHAR(100) NULL,
        Drivetrain           NVARCHAR(100) NULL,
        SourceUrl            NVARCHAR(1000) NULL,
        ConfidenceScore      DECIMAL(5,2) NOT NULL CONSTRAINT DF_VEPC_ConfidenceScore DEFAULT 0,
        Status               NVARCHAR(50) NOT NULL CONSTRAINT DF_VEPC_Status DEFAULT 'CandidateFromVehicleSpec',
        CreatedAt            DATETIME2 NOT NULL CONSTRAINT DF_VEPC_CreatedAt DEFAULT SYSUTCDATETIME(),
        ApprovedByAdmin      BIT NOT NULL CONSTRAINT DF_VEPC_ApprovedByAdmin DEFAULT 0,
        Notes                NVARCHAR(1000) NULL,
        CONSTRAINT FK_VehicleExpectedPartCandidates_UsedCars FOREIGN KEY (DonorVehicleId) REFERENCES dbo.UsedCars (Id)
    );
    CREATE INDEX IX_VehicleExpectedPartCandidates_Vehicle ON dbo.VehicleExpectedPartCandidates (DonorVehicleId);
    CREATE INDEX IX_VehicleExpectedPartCandidates_Status ON dbo.VehicleExpectedPartCandidates (Status);
END;
GO
