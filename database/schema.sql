-- SpareParts Database Schema
-- Generated for staging deployment

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ── Table-Valued Parameter type ───────────────────────────────────────────────
IF TYPE_ID('dbo.IntIdList') IS NULL
    CREATE TYPE dbo.IntIdList AS TABLE (Id INT NOT NULL);
GO

-- ── AppConstants ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.AppConstants', 'U') IS NULL
CREATE TABLE dbo.AppConstants (
    [Key]         NVARCHAR(255) NOT NULL PRIMARY KEY,
    [Value]       NVARCHAR(MAX) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    UpdatedAt     DATETIME2     NOT NULL CONSTRAINT DF_AppConstants_UpdatedAt DEFAULT SYSUTCDATETIME()
);
GO

-- Backfill DEFAULT on UpdatedAt for existing databases that lack it
IF NOT EXISTS (
    SELECT 1 FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE OBJECT_NAME(dc.parent_object_id) = 'AppConstants' AND c.name = 'UpdatedAt'
)
    ALTER TABLE dbo.AppConstants
        ADD CONSTRAINT DF_AppConstants_UpdatedAt DEFAULT SYSUTCDATETIME() FOR UpdatedAt;
GO

-- ── Brands ────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Brands', 'U') IS NULL
CREATE TABLE dbo.Brands (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(120) NOT NULL,
    IsActive          BIT           NOT NULL DEFAULT 1,
    CreatedAt         DATETIME2     NOT NULL,
    ModifiedAt        DATETIME2     NULL,
    CreatedByUserId   INT           NULL,
    ModifiedByUserId  INT           NULL
);
GO

-- ── Categories ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
CREATE TABLE dbo.Categories (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(120) NOT NULL,
    ParentId          INT           NULL,
    CreatedAt         DATETIME2     NOT NULL,
    ModifiedAt        DATETIME2     NULL,
    CreatedByUserId   INT           NULL,
    ModifiedByUserId  INT           NULL
);
GO

-- ── CarBrands ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.CarBrands', 'U') IS NULL
CREATE TABLE dbo.CarBrands (
    Id                INT              NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(120)    NOT NULL,
    Country           NVARCHAR(80)     NULL,
    RegionGroup       NVARCHAR(80)     NULL,
    LogoData          VARBINARY(MAX)   NULL,
    LogoMimeType      NVARCHAR(50)     NULL,
    IsActive          BIT              NOT NULL DEFAULT 1,
    SortOrder         INT              NOT NULL DEFAULT 0,
    CreatedAt         DATETIME2        NOT NULL,
    ModifiedAt        DATETIME2        NULL,
    CreatedByUserId   INT              NULL,
    ModifiedByUserId  INT              NULL
);
GO

-- ── CarModels ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.CarModels', 'U') IS NULL
CREATE TABLE dbo.CarModels (
    Id                INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CarBrandId        INT            NOT NULL,
    Name              NVARCHAR(120)  NOT NULL,
    Year              NVARCHAR(10)   NULL,
    EngineType        NVARCHAR(80)   NULL,
    BasePrice         DECIMAL(19,4)  NOT NULL DEFAULT 0,
    ImageData         VARBINARY(MAX) NULL,
    ImageMimeType     NVARCHAR(50)   NULL,
    IsActive          BIT            NOT NULL DEFAULT 1,
    CreatedAt         DATETIME2      NOT NULL,
    ModifiedAt        DATETIME2      NULL,
    CreatedByUserId   INT            NULL,
    ModifiedByUserId  INT            NULL,
    BodyType          NVARCHAR(80)   NULL,
    CONSTRAINT FK_CarModels_CarBrands FOREIGN KEY (CarBrandId) REFERENCES dbo.CarBrands(Id)
);
GO

-- ── CurrencyRates ─────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.CurrencyRates', 'U') IS NULL
CREATE TABLE dbo.CurrencyRates (
    Code          NVARCHAR(10)  NOT NULL PRIMARY KEY,
    RateToUsd     DECIMAL(19,8) NOT NULL DEFAULT 1,
    BaseCode      NVARCHAR(10)  NOT NULL,
    SnapshotUtc   DATETIME2     NOT NULL
);
GO

-- ── Warehouses ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Warehouses', 'U') IS NULL
CREATE TABLE dbo.Warehouses (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(120) NOT NULL,
    Address           NVARCHAR(255) NULL,
    IsMain            BIT           NOT NULL DEFAULT 0,
    CreatedAt         DATETIME2     NOT NULL,
    ModifiedAt        DATETIME2     NULL,
    CreatedByUserId   INT           NULL,
    ModifiedByUserId  INT           NULL,
    Barcode           NVARCHAR(50)  NULL
);
GO

-- ── Location (shipping / used-car origin) ─────────────────────────────────────
IF OBJECT_ID('dbo.Location', 'U') IS NULL
CREATE TABLE dbo.Location (
    LocationID                 INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name                       NVARCHAR(120) NOT NULL,
    ShippingFees               DECIMAL(19,4) NOT NULL DEFAULT 0,
    ShippingFeesCurrencyCode   NVARCHAR(10)  NOT NULL CONSTRAINT DF_Location_ShippingFeesCurrencyCode DEFAULT 'USD',
    CreatedAt                  DATETIME2     NOT NULL,
    CreatedByUserId            INT           NULL,
    ModifiedAt                 DATETIME2     NULL,
    ModifiedByUserId           INT           NULL
);
GO

-- ── Locations (warehouse shelf positions) ────────────────────────────────────
IF OBJECT_ID('dbo.Locations', 'U') IS NULL
CREATE TABLE dbo.Locations (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    WarehouseId       INT           NOT NULL,
    Code              NVARCHAR(50)  NOT NULL,
    Description       NVARCHAR(255) NULL,
    IsActive          BIT           NOT NULL DEFAULT 1,
    CreatedAt         DATETIME2     NOT NULL,
    ModifiedAt        DATETIME2     NULL,
    CreatedByUserId   INT           NULL,
    ModifiedByUserId  INT           NULL,
    CONSTRAINT FK_Locations_Warehouses FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(Id)
);
GO

-- ── Roles ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
CREATE TABLE dbo.Roles (
    Id             INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name           NVARCHAR(100) NOT NULL,
    Description    NVARCHAR(255) NULL,
    BadgeColor     NVARCHAR(20)  NULL,
    BadgeTextColor NVARCHAR(20)  NULL,
    IsSystem       BIT           NOT NULL DEFAULT 0,
    IsActive       BIT           NOT NULL DEFAULT 1,
    CreatedAt      DATETIME2     NOT NULL,
    ModifiedAt     DATETIME2     NULL
);
GO

-- Add missing columns if Roles was created with an older schema
IF COL_LENGTH('dbo.Roles', 'Description') IS NULL
    ALTER TABLE dbo.Roles ADD Description NVARCHAR(255) NULL;
GO
IF COL_LENGTH('dbo.Roles', 'BadgeColor') IS NULL
    ALTER TABLE dbo.Roles ADD BadgeColor NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Roles', 'BadgeTextColor') IS NULL
    ALTER TABLE dbo.Roles ADD BadgeTextColor NVARCHAR(20) NULL;
GO
IF COL_LENGTH('dbo.Roles', 'IsSystem') IS NULL
    ALTER TABLE dbo.Roles ADD IsSystem BIT NOT NULL CONSTRAINT DF_Roles_IsSystem DEFAULT 0;
GO
IF COL_LENGTH('dbo.Roles', 'ModifiedAt') IS NULL
    ALTER TABLE dbo.Roles ADD ModifiedAt DATETIME2 NULL;
GO

-- ── Users ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Users', 'U') IS NULL
CREATE TABLE dbo.Users (
    Id              INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Username        NVARCHAR(100) NOT NULL,
    FullName        NVARCHAR(160) NOT NULL,
    Email           NVARCHAR(255) NOT NULL,
    PasswordHash    NVARCHAR(MAX) NOT NULL,
    RoleId          INT           NOT NULL DEFAULT 1,
    IsActive        BIT           NOT NULL DEFAULT 1,
    LastLoginAt     DATETIME2     NULL,
    CreatedAt       DATETIME2     NOT NULL,
    ModifiedAt      DATETIME2     NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
);
GO

-- ── Customers ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Customers', 'U') IS NULL
CREATE TABLE dbo.Customers (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(160) NOT NULL,
    Phone             NVARCHAR(50)  NULL,
    Email             NVARCHAR(255) NULL,
    Address           NVARCHAR(500) NULL,
    TaxNumber         NVARCHAR(50)  NULL,
    OpeningBalance    DECIMAL(19,4) NOT NULL DEFAULT 0,
    AccountId         INT           NULL,
    CreatedAt         DATETIME2     NOT NULL,
    CreatedByUserId   INT           NULL,
    ModifiedAt        DATETIME2     NULL,
    ModifiedByUserId  INT           NULL
);
GO

-- ── Suppliers ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Suppliers', 'U') IS NULL
CREATE TABLE dbo.Suppliers (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(160) NOT NULL,
    Phone             NVARCHAR(50)  NULL,
    Email             NVARCHAR(255) NULL,
    Address           NVARCHAR(500) NULL,
    TaxNumber         NVARCHAR(50)  NULL,
    OpeningBalance    DECIMAL(19,4) NOT NULL DEFAULT 0,
    AccountId         INT           NULL,
    CreatedAt         DATETIME2     NOT NULL,
    CreatedByUserId   INT           NULL,
    ModifiedAt        DATETIME2     NULL,
    ModifiedByUserId  INT           NULL
);
GO

-- ── UsedCars ──────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCars', 'U') IS NULL
CREATE TABLE dbo.UsedCars (
    Id                       INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    CarModelId               INT           NOT NULL,
    ModelYear                INT           NOT NULL,
    PriceCurrency            NVARCHAR(10)  NOT NULL,
    Price                    DECIMAL(19,4) NOT NULL DEFAULT 0,
    PriceBase                DECIMAL(19,4) NOT NULL DEFAULT 0,
    PriceCounter             DECIMAL(19,4) NOT NULL DEFAULT 0,
    LocationId               INT           NULL,
    Location                 NVARCHAR(120) NULL,
    Transportation           DECIMAL(19,4) NOT NULL DEFAULT 0,
    PartOutAmount            DECIMAL(19,4) NOT NULL DEFAULT 0,
    Shipping                 DECIMAL(19,4) NOT NULL DEFAULT 0,
    Customs                  DECIMAL(19,4) NOT NULL DEFAULT 0,
    Repairs                  DECIMAL(19,4) NOT NULL DEFAULT 0,
    TotalBeforeShipping      DECIMAL(19,4) NOT NULL DEFAULT 0,
    GrandTotalBase           DECIMAL(19,4) NOT NULL DEFAULT 0,
    GrandTotalCounter        DECIMAL(19,4) NOT NULL DEFAULT 0,
    BaseCurrencyCode         NVARCHAR(10)  NULL,
    CounterCurrencyCode      NVARCHAR(10)  NULL,
    CounterRateToBase        DECIMAL(19,8) NOT NULL DEFAULT 1,
    IsReceived               BIT           NOT NULL DEFAULT 0,
    IsShipped                BIT           NOT NULL DEFAULT 0,
    PartOut                  BIT           NULL,
    ReceivedAt               DATETIME2     NULL,
    SupplierId               INT           NULL,
    Barcode                  NVARCHAR(50)  NULL,
    ExpectedSellThroughRate  DECIMAL(19,4) NOT NULL DEFAULT 0,
    CreatedAt                DATETIME2     NOT NULL,
    ModifiedAt               DATETIME2     NULL,
    CreatedByUserId          INT           NULL,
    ModifiedByUserId         INT           NULL,
    CONSTRAINT FK_UsedCars_CarModels FOREIGN KEY (CarModelId) REFERENCES dbo.CarModels(Id)
);
GO

-- ── usedcar_images ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.usedcar_images', 'U') IS NULL
CREATE TABLE dbo.usedcar_images (
    ImageId         INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    UsedCarId       INT            NOT NULL,
    ImageMimeType   NVARCHAR(50)   NOT NULL,
    ImageData       VARBINARY(MAX) NOT NULL,
    CreatedAt       DATETIME2      NOT NULL,
    CONSTRAINT FK_usedcar_images_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars(Id)
);
GO

-- ── Parts ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Parts', 'U') IS NULL
CREATE TABLE dbo.Parts (
    Id                      INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    InternalCode            NVARCHAR(50)  NOT NULL,
    Barcode                 NVARCHAR(50)  NULL,
    Name                    NVARCHAR(160) NOT NULL,
    OEMNumber               NVARCHAR(80)  NULL,
    Condition               INT           NOT NULL DEFAULT 1,
    CategoryId              INT           NULL,
    BrandId                 INT           NULL,
    CostPrice               DECIMAL(19,4) NOT NULL DEFAULT 0,
    SalePrice               DECIMAL(19,4) NOT NULL DEFAULT 0,
    AveragePrice            DECIMAL(19,4) NULL,
    EstimatedMarketPrice    DECIMAL(19,4) NULL,
    CostAllocationPercent   DECIMAL(19,4) NOT NULL DEFAULT 0,
    AllocatedCost           DECIMAL(19,4) NOT NULL DEFAULT 0,
    MinimumSellPrice        DECIMAL(19,4) NOT NULL DEFAULT 0,
    FastSalePrice           DECIMAL(19,4) NOT NULL DEFAULT 0,
    WholesalePrice          DECIMAL(19,4) NOT NULL DEFAULT 0,
    RecommendedPrice        DECIMAL(19,4) NOT NULL DEFAULT 0,
    PricingStatus           NVARCHAR(50)  NOT NULL DEFAULT 'Manual',
    PricingCalculatedAt     DATETIME2     NULL,
    Currency                NVARCHAR(10)  NOT NULL DEFAULT 'USD',
    MinStock                INT           NOT NULL DEFAULT 0,
    Notes                   NVARCHAR(MAX) NULL,
    UsedCarID               INT           NULL,
    IsActive                BIT           NOT NULL DEFAULT 1,
    CreatedAt               DATETIME2     NOT NULL,
    ModifiedAt              DATETIME2     NULL,
    CreatedByUserId         INT           NULL,
    ModifiedByUserId        INT           NULL,
    CONSTRAINT FK_Parts_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id),
    CONSTRAINT FK_Parts_Brands     FOREIGN KEY (BrandId)    REFERENCES dbo.Brands(Id),
    CONSTRAINT FK_Parts_UsedCars   FOREIGN KEY (UsedCarID)  REFERENCES dbo.UsedCars(Id)
);
GO

-- ── Stock ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Stock', 'U') IS NULL
CREATE TABLE dbo.Stock (
    Id                INT  NOT NULL IDENTITY(1,1) PRIMARY KEY,
    PartId            INT  NOT NULL,
    WarehouseId       INT  NOT NULL,
    LocationId        INT  NULL,
    Quantity          INT  NOT NULL DEFAULT 0,
    ReservedQuantity  INT  NOT NULL DEFAULT 0,
    CreatedAt         DATETIME2 NOT NULL,
    ModifiedAt        DATETIME2 NULL,
    CreatedByUserId   INT  NULL,
    ModifiedByUserId  INT  NULL,
    CONSTRAINT FK_Stock_Parts      FOREIGN KEY (PartId)      REFERENCES dbo.Parts(Id),
    CONSTRAINT FK_Stock_Warehouses FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses(Id)
);
GO

-- ── StockMovements ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.StockMovements', 'U') IS NULL
CREATE TABLE dbo.StockMovements (
    Id              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    PartId          INT            NOT NULL,
    WarehouseId     INT            NOT NULL,
    Quantity        INT            NOT NULL,
    MovementType    INT            NOT NULL,
    ReferenceType   NVARCHAR(50)   NOT NULL,
    ReferenceId     INT            NULL,
    UnitCost        DECIMAL(19,4)  NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2      NOT NULL,
    CreatedByUserId INT            NULL,
    ScanCode        NVARCHAR(50)   NULL
);
GO

-- ── Accounts ──────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Accounts', 'U') IS NULL
CREATE TABLE dbo.Accounts (
    Id                INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Code              NVARCHAR(20)  NOT NULL,
    Name              NVARCHAR(160) NOT NULL,
    AccountType       NVARCHAR(50)  NULL,
    AccountTypeKey    NVARCHAR(40)  NULL,
    ParentId          INT           NULL,
    CreatedAt         DATETIME2     NOT NULL,
    ModifiedAt        DATETIME2     NULL,
    CreatedByUserId   INT           NULL,
    ModifiedByUserId  INT           NULL
);
GO

-- ── AccountingAccountTypes ────────────────────────────────────────────────────
IF OBJECT_ID('dbo.AccountingAccountTypes', 'U') IS NULL
CREATE TABLE dbo.AccountingAccountTypes (
    TypeKey     NVARCHAR(40)  NOT NULL PRIMARY KEY,
    Label       NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1
);
GO

-- Resize AccountingAccountTypes.TypeKey from NVARCHAR(50) to NVARCHAR(40) on
-- existing DBs so it matches what AccountingMigration expects for the FK.
-- Only runs if no FK already references this column (safe resize window).
IF OBJECT_ID('dbo.AccountingAccountTypes', 'U') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID('dbo.AccountingAccountTypes')
         AND name = 'TypeKey' AND max_length = 100  -- NVARCHAR(50) = 100 bytes
   )
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys
       WHERE referenced_object_id = OBJECT_ID('dbo.AccountingAccountTypes')
   )
BEGIN
    DECLARE @pkName NVARCHAR(200);
    SELECT @pkName = name FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID('dbo.AccountingAccountTypes') AND type = 'PK';
    EXEC('ALTER TABLE dbo.AccountingAccountTypes DROP CONSTRAINT [' + @pkName + ']');
    ALTER TABLE dbo.AccountingAccountTypes ALTER COLUMN TypeKey NVARCHAR(40) NOT NULL;
    ALTER TABLE dbo.AccountingAccountTypes ADD CONSTRAINT PK_AccountingAccountTypes PRIMARY KEY CLUSTERED (TypeKey);
END
GO

-- Resize Accounts.AccountTypeKey from NVARCHAR(50) to NVARCHAR(40) on existing
-- DBs that were created before this fix (no-op if already NVARCHAR(40)).
IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID('dbo.Accounts')
         AND name = 'AccountTypeKey' AND max_length = 100  -- NVARCHAR(50)
   )
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Accounts_AccountTypeKey')
BEGIN
    ALTER TABLE dbo.Accounts ALTER COLUMN AccountTypeKey NVARCHAR(40) NULL;
END
GO

-- ── AccountingPostingSettings ─────────────────────────────────────────────────
IF OBJECT_ID('dbo.AccountingPostingSettings', 'U') IS NULL
CREATE TABLE dbo.AccountingPostingSettings (
    SettingKey        NVARCHAR(80) NOT NULL PRIMARY KEY,
    AccountId         INT          NOT NULL,
    ModifiedAt        DATETIME2    NULL,
    ModifiedByUserId  INT          NULL
);
GO

-- ── AccountingPostingRoles ────────────────────────────────────────────────────
IF OBJECT_ID('dbo.AccountingPostingRoles', 'U') IS NULL
CREATE TABLE dbo.AccountingPostingRoles (
    RoleKey     NVARCHAR(80)  NOT NULL PRIMARY KEY,
    Label       NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1
);
GO

-- Resize RoleKey from NVARCHAR(50) to NVARCHAR(80) on existing databases
IF COL_LENGTH('dbo.AccountingPostingRoles', 'RoleKey') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.AccountingPostingRoles'), 'RoleKey', 'CharMaxLen') < 80
BEGIN
    -- Drop FK that references this PK
    IF OBJECT_ID('dbo.FK_AccountingPostingSettings_Roles', 'F') IS NOT NULL
        ALTER TABLE dbo.AccountingPostingSettings DROP CONSTRAINT FK_AccountingPostingSettings_Roles;

    -- Drop existing PK (name may be auto-generated)
    DECLARE @pkName NVARCHAR(256);
    SELECT @pkName = kc.name
    FROM   sys.key_constraints kc
    JOIN   sys.tables t ON t.object_id = kc.parent_object_id
    WHERE  t.name = 'AccountingPostingRoles'
      AND  kc.type = 'PK';
    IF @pkName IS NOT NULL
        EXEC('ALTER TABLE dbo.AccountingPostingRoles DROP CONSTRAINT [' + @pkName + ']');

    ALTER TABLE dbo.AccountingPostingRoles ALTER COLUMN RoleKey NVARCHAR(80) NOT NULL;
    ALTER TABLE dbo.AccountingPostingRoles ADD CONSTRAINT PK_AccountingPostingRoles PRIMARY KEY (RoleKey);

    -- Re-add FK
    ALTER TABLE dbo.AccountingPostingSettings ADD CONSTRAINT FK_AccountingPostingSettings_Roles
        FOREIGN KEY (SettingKey) REFERENCES dbo.AccountingPostingRoles(RoleKey);
END;
GO

-- ── JournalEntries ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.JournalEntries', 'U') IS NULL
CREATE TABLE dbo.JournalEntries (
    Id               INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    EntryDate        DATETIME2     NOT NULL,
    ReferenceType    NVARCHAR(50)  NULL,
    ReferenceId      INT           NULL,
    Description      NVARCHAR(255) NULL,
    CreatedAt        DATETIME2     NOT NULL,
    CreatedByUserId  INT           NULL
);
GO

-- ── JournalLines ─────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.JournalLines', 'U') IS NULL
CREATE TABLE dbo.JournalLines (
    Id                  INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    JournalEntryId      INT           NOT NULL,
    AccountId           INT           NOT NULL,
    Debit               DECIMAL(19,4) NOT NULL DEFAULT 0,
    Credit              DECIMAL(19,4) NOT NULL DEFAULT 0,
    CurrencyCode        NVARCHAR(10)  NULL,
    OriginalAmount      DECIMAL(19,4) NULL,
    RateToBase          DECIMAL(19,8) NULL,
    CounterAmount       DECIMAL(19,4) NULL,
    BaseCurrencyCode    NVARCHAR(10)  NULL,
    CounterCurrencyCode NVARCHAR(10)  NULL,
    CreatedAt           DATETIME2     NOT NULL,
    CreatedByUserId     INT           NULL,
    CONSTRAINT FK_JournalLines_JournalEntries FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(Id),
    CONSTRAINT FK_JournalLines_Accounts       FOREIGN KEY (AccountId)      REFERENCES dbo.Accounts(Id)
);
GO

-- ── TransactionTypes ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.TransactionTypes', 'U') IS NULL
CREATE TABLE dbo.TransactionTypes (
    Id                    INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
    TypeKey               NVARCHAR(80)   NOT NULL,
    Name                  NVARCHAR(120)  NOT NULL,
    CurrencyCode          NVARCHAR(10)   NOT NULL DEFAULT 'USD',
    CounterRate           DECIMAL(19,8)  NOT NULL DEFAULT 1,
    SerialNumberFormat    NVARCHAR(80)   NOT NULL DEFAULT '{0}',
    SerialStartNumber     BIGINT         NOT NULL DEFAULT 1,
    SerialCurrentNumber   BIGINT         NOT NULL DEFAULT 0,
    IsActive              BIT            NOT NULL DEFAULT 1,
    SortOrder             INT            NOT NULL DEFAULT 10
);
GO

-- ── Transactions ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
CREATE TABLE dbo.Transactions (
    Id                   INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    TransactionTypeId    INT           NOT NULL,
    ReferenceId          INT           NOT NULL DEFAULT 0,
    TransactionNumber    NVARCHAR(50)  NOT NULL,
    ScanCode             NVARCHAR(50)  NULL,
    TransactionDate      DATETIME2     NOT NULL,
    CustomerId           INT           NULL,
    SupplierId           INT           NULL,
    UsedCarId            INT           NULL,
    WarehouseId          INT           NULL,
    Subtotal             DECIMAL(19,4) NOT NULL DEFAULT 0,
    DiscountAmount       DECIMAL(19,4) NOT NULL DEFAULT 0,
    TaxAmount            DECIMAL(19,4) NOT NULL DEFAULT 0,
    TotalAmount          DECIMAL(19,4) NOT NULL DEFAULT 0,
    PaidAmount           DECIMAL(19,4) NOT NULL DEFAULT 0,
    PaymentStatus        NVARCHAR(40)  NOT NULL DEFAULT 'Unpaid',
    PaymentMethod        NVARCHAR(50)  NULL,
    Notes                NVARCHAR(MAX) NULL,
    IsReturn             BIT           NOT NULL DEFAULT 0,
    ParentReferenceId    INT           NULL,
    TotalCost            DECIMAL(19,4) NOT NULL DEFAULT 0,
    BaseCurrencyCode     NVARCHAR(10)  NULL,
    CounterCurrencyCode  NVARCHAR(10)  NULL,
    TotalBaseAmount      DECIMAL(19,4) NOT NULL DEFAULT 0,
    TotalCounterAmount   DECIMAL(19,4) NOT NULL DEFAULT 0,
    PaidCounterAmount    DECIMAL(19,4) NOT NULL DEFAULT 0,
    PostingStatus        NVARCHAR(40)  NULL,
    PostedAt             DATETIME2     NULL,
    PostedByUserId       INT           NULL,
    ModifiedAt           DATETIME2     NULL,
    ModifiedByUserId     INT           NULL,
    CreatedAt            DATETIME2     NOT NULL,
    CreatedByUserId      INT           NULL,
    CONSTRAINT FK_Transactions_TransactionTypes FOREIGN KEY (TransactionTypeId) REFERENCES dbo.TransactionTypes(Id)
);
GO

-- ── TransactionItems ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.TransactionItems', 'U') IS NULL
CREATE TABLE dbo.TransactionItems (
    Id               INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    TransactionId    INT           NOT NULL,
    ItemType         NVARCHAR(50)  NOT NULL,
    PartId           INT           NULL,
    AccountId        INT           NULL,
    Description      NVARCHAR(MAX) NULL,
    Quantity         DECIMAL(19,4) NOT NULL DEFAULT 1,
    UnitPrice        DECIMAL(19,4) NULL,
    UnitCost         DECIMAL(19,4) NULL,
    DiscountAmount   DECIMAL(19,4) NOT NULL DEFAULT 0,
    TaxRate          DECIMAL(19,4) NOT NULL DEFAULT 0,
    Amount           DECIMAL(19,4) NOT NULL DEFAULT 0,
    LineTotal        DECIMAL(19,4) NOT NULL DEFAULT 0,
    CurrencyCode     NVARCHAR(10)  NOT NULL DEFAULT 'USD',
    RateToBase       DECIMAL(19,8) NOT NULL DEFAULT 1,
    BaseAmount       DECIMAL(19,4) NOT NULL DEFAULT 0,
    CounterAmount    DECIMAL(19,4) NOT NULL DEFAULT 0,
    SortOrder        INT           NOT NULL DEFAULT 0,
    CreatedAt        DATETIME2     NOT NULL,
    CreatedByUserId  INT           NULL,
    ModifiedAt       DATETIME2     NULL,
    ModifiedByUserId INT           NULL,
    CONSTRAINT FK_TransactionItems_Transactions FOREIGN KEY (TransactionId) REFERENCES dbo.Transactions(Id)
);
GO

-- ── ExcelImportMetadata ───────────────────────────────────────────────────────
IF OBJECT_ID('dbo.ExcelImportMetadata', 'U') IS NULL
CREATE TABLE dbo.ExcelImportMetadata (
    Id             INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    TargetTable    NVARCHAR(80)  NOT NULL,
    ColumnMappings NVARCHAR(MAX) NULL,
    CreatedAt      DATETIME2     NOT NULL
);
GO

-- ── OutboundMessages ──────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.OutboundMessages', 'U') IS NULL
CREATE TABLE dbo.OutboundMessages (
    Id                  INT           NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Direction           NVARCHAR(20)  NOT NULL,
    Channel             NVARCHAR(50)  NOT NULL,
    RecipientKind       NVARCHAR(50)  NOT NULL,
    RecipientId         INT           NULL,
    RecipientName       NVARCHAR(160) NULL,
    RecipientPhone      NVARCHAR(50)  NULL,
    TemplateKey         NVARCHAR(80)  NULL,
    ReferenceType       NVARCHAR(50)  NULL,
    ReferenceId         INT           NULL,
    Body                NVARCHAR(MAX) NULL,
    AttachmentCount     INT           NOT NULL DEFAULT 0,
    Status              NVARCHAR(40)  NOT NULL,
    Provider            NVARCHAR(80)  NULL,
    ProviderMessageId   NVARCHAR(255) NULL,
    ProviderStatus      NVARCHAR(80)  NULL,
    ErrorMessage        NVARCHAR(MAX) NULL,
    CreatedAt           DATETIME2     NOT NULL,
    CreatedByUserId     INT           NULL,
    SentAt              DATETIME2     NULL
);
GO

-- ── Seed: Roles ───────────────────────────────────────────────────────────────
SET IDENTITY_INSERT dbo.Roles ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 1)
    INSERT INTO dbo.Roles (Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive, CreatedAt)
    VALUES (1, 'Admin', 'Full system access', '#22FF5722', '#FF7043', 1, 1, SYSUTCDATETIME());
SET IDENTITY_INSERT dbo.Roles OFF;
GO

-- ── Seed: Default admin user (password: Admin@123) ───────────────────────────
-- PasswordHash is BCrypt of "Admin@123"
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
    INSERT INTO dbo.Users (Username, FullName, Email, PasswordHash, RoleId, IsActive, CreatedAt)
    VALUES ('admin', 'Administrator', 'admin@spareparts.local',
            '$2b$11$ehs8XKNePVFgQm6pbnllAO/4PKiCRP2MAquqW/E4AnJjN4lOJlcRC',
            1, 1, SYSUTCDATETIME());
GO

-- ── Seed: Default warehouse ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.Warehouses)
    INSERT INTO dbo.Warehouses (Name, IsMain, CreatedAt, Barcode)
    VALUES ('Main Warehouse', 1, SYSUTCDATETIME(), 'WH-1');
GO

-- ── Seed: AppConstants ────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'BaseCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], [Description], UpdatedAt) VALUES
        ('BaseCurrencyCode',            'USD',    'Application base currency code.',                         SYSUTCDATETIME()),
        ('CounterCurrencyCode',         'USD',    'Application counter currency code.',                      SYSUTCDATETIME()),
        ('DefaultCurrencyCode',         'USD',    'Fallback invoice currency code.',                         SYSUTCDATETIME()),
        ('DefaultCounterRate',          '1',      'Fallback counter/base rate.',                             SYSUTCDATETIME()),
        ('DisplayCurrencyCode',         'USD',    'Application display currency code.',                      SYSUTCDATETIME()),
        ('DefaultSalesTransactionTypeName', 'Sales', 'Default transaction type for invoices.',               SYSUTCDATETIME());
END;
GO

-- ── Seed: TransactionTypes ────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'sale')
BEGIN
    INSERT INTO dbo.TransactionTypes
        (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES
        ('sale',              'Sales',              'USD', 1, 'INV-{0:D6}',  1, 0, 1, 10),
        ('sale_return',       'Sales Return',       'USD', 1, 'RET-{0:D6}',  1, 0, 1, 20),
        ('purchase',          'Purchase',           'USD', 1, 'PO-{0:D6}',   1, 0, 1, 30),
        ('used_car_purchase', 'Used Car Purchase',  'USD', 1, 'UCP-{0:D6}',  1, 0, 1, 40);
END;
GO

-- ── Seed: Accounting account types ───────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM dbo.AccountingAccountTypes WHERE TypeKey = 'asset')
BEGIN
    INSERT INTO dbo.AccountingAccountTypes (TypeKey, Label, Description, SortOrder, IsActive) VALUES
        ('asset',     'Asset',     'Assets',                        10, 1),
        ('liability', 'Liability', 'Liabilities',                   20, 1),
        ('equity',    'Equity',    'Equity accounts',               30, 1),
        ('revenue',   'Revenue',   'Revenue and income accounts',   40, 1),
        ('expense',   'Expense',   'Expense accounts',              50, 1);
END;
GO

-- Ensure 'income' key exists (migration uses 'income', schema seeds 'revenue')
IF NOT EXISTS (SELECT 1 FROM dbo.AccountingAccountTypes WHERE TypeKey = 'income')
    INSERT INTO dbo.AccountingAccountTypes (TypeKey, Label, Description, SortOrder, IsActive)
    VALUES ('income', 'Income', 'Revenue and income accounts.', 40, 1);
GO

-- ════════════════════════════════════════════════════════════════════════════
-- MIGRATIONS (moved from API startup — all idempotent, safe to re-run)
-- ════════════════════════════════════════════════════════════════════════════

-- ── InvoiceNumberingMigration ─────────────────────────────────────────────
IF OBJECT_ID('dbo.SalesInvoiceNumberSequence', 'SO') IS NULL
BEGIN
    CREATE SEQUENCE dbo.SalesInvoiceNumberSequence
        AS BIGINT
        START WITH 1
        INCREMENT BY 1
        MINVALUE 1
        NO MAXVALUE
        CACHE 50;
END;

IF OBJECT_ID('dbo.PurchaseInvoiceNumberSequence', 'SO') IS NULL
BEGIN
    CREATE SEQUENCE dbo.PurchaseInvoiceNumberSequence
        AS BIGINT
        START WITH 1
        INCREMENT BY 1
        MINVALUE 1
        NO MAXVALUE
        CACHE 50;
END;

IF OBJECT_ID('dbo.SalesInvoices', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoices', 'InvoiceNumber') IS NOT NULL
   AND COL_LENGTH('dbo.SalesInvoices', 'InvoiceNumber') < 64
BEGIN
    ALTER TABLE dbo.SalesInvoices ALTER COLUMN InvoiceNumber NVARCHAR(32) NOT NULL;
END;

IF OBJECT_ID('dbo.PurchaseInvoices', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.PurchaseInvoices', 'PurchaseNumber') IS NOT NULL
   AND COL_LENGTH('dbo.PurchaseInvoices', 'PurchaseNumber') < 64
BEGIN
    ALTER TABLE dbo.PurchaseInvoices ALTER COLUMN PurchaseNumber NVARCHAR(32) NOT NULL;
END;

IF OBJECT_ID('dbo.SalesInvoices', 'U') IS NOT NULL
    AND NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.SalesInvoices')
          AND name = 'UX_SalesInvoices_InvoiceNumber'
    )
BEGIN
    CREATE UNIQUE INDEX UX_SalesInvoices_InvoiceNumber
        ON dbo.SalesInvoices(InvoiceNumber);
END;

IF OBJECT_ID('dbo.PurchaseInvoices', 'U') IS NOT NULL
    AND NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID('dbo.PurchaseInvoices')
          AND name = 'UX_PurchaseInvoices_PurchaseNumber'
    )
BEGIN
    CREATE UNIQUE INDEX UX_PurchaseInvoices_PurchaseNumber
        ON dbo.PurchaseInvoices(PurchaseNumber);
END;
GO

-- ── AccountingMigration ───────────────────────────────────────────────────
IF OBJECT_ID('dbo.AccountingAccountTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountingAccountTypes
    (
        TypeKey NVARCHAR(40) NOT NULL PRIMARY KEY,
        Label NVARCHAR(80) NOT NULL,
        Description NVARCHAR(255) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_AccountingAccountTypes_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AccountingAccountTypes_IsActive DEFAULT (1)
    );
END;

MERGE dbo.AccountingAccountTypes AS target
USING
(
    VALUES
        ('asset', 'Asset', 'Resources owned or controlled by the business.', 10, 1),
        ('liability', 'Liability', 'Amounts owed by the business.', 20, 1),
        ('equity', 'Equity', 'Owner equity and retained balances.', 30, 1),
        ('income', 'Income', 'Revenue and income accounts.', 40, 1),
        ('expense', 'Expense', 'Expense and cost accounts.', 50, 1)
) AS source (TypeKey, Label, Description, SortOrder, IsActive)
ON target.TypeKey = source.TypeKey
WHEN MATCHED THEN
    UPDATE SET Label = source.Label,
               Description = source.Description,
               SortOrder = source.SortOrder,
               IsActive = source.IsActive
WHEN NOT MATCHED THEN
    INSERT (TypeKey, Label, Description, SortOrder, IsActive)
    VALUES (source.TypeKey, source.Label, source.Description, source.SortOrder, source.IsActive);

IF OBJECT_ID('dbo.Accounts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Accounts
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Code NVARCHAR(20) NOT NULL UNIQUE,
        Name NVARCHAR(160) NOT NULL,
        AccountType INT NULL,
        AccountTypeKey NVARCHAR(40) NOT NULL,
        ParentId INT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Accounts_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Accounts_Parent FOREIGN KEY (ParentId) REFERENCES dbo.Accounts(Id)
    );
END;

IF COL_LENGTH('dbo.Accounts', 'AccountTypeKey') IS NULL
BEGIN
    ALTER TABLE dbo.Accounts ADD AccountTypeKey NVARCHAR(40) NULL;
END;

IF COL_LENGTH('dbo.Accounts', 'AccountType') IS NOT NULL
BEGIN
    BEGIN TRY
        ALTER TABLE dbo.Accounts ALTER COLUMN AccountType INT NULL;
    END TRY
    BEGIN CATCH
    END CATCH
END;

UPDATE dbo.Accounts
SET AccountTypeKey = CASE
    WHEN AccountTypeKey IS NOT NULL AND LTRIM(RTRIM(AccountTypeKey)) <> '' THEN LOWER(LTRIM(RTRIM(AccountTypeKey)))
    WHEN AccountType IS NULL THEN 'asset'
    WHEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), AccountType)) = 0 THEN 'asset'
    WHEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), AccountType)) = 1 THEN 'liability'
    WHEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), AccountType)) = 2 THEN 'equity'
    WHEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), AccountType)) = 3 THEN 'income'
    WHEN TRY_CONVERT(INT, CONVERT(NVARCHAR(50), AccountType)) = 4 THEN 'expense'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), AccountType))), ' ', ''), '_', ''), '-', '')) IN ('ASSET', 'ASSETS') THEN 'asset'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), AccountType))), ' ', ''), '_', ''), '-', '')) IN ('LIABILITY', 'LIABILITIES') THEN 'liability'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), AccountType))), ' ', ''), '_', ''), '-', '')) = 'EQUITY' THEN 'equity'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), AccountType))), ' ', ''), '_', ''), '-', '')) IN ('INCOME', 'REVENUE', 'REVENUES', 'SALESREVENUE') THEN 'income'
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), AccountType))), ' ', ''), '_', ''), '-', '')) IN ('EXPENSE', 'EXPENSES', 'COGS', 'COSTOFGOODSSOLD', 'COSTOFSALES') THEN 'expense'
    ELSE 'asset'
END
WHERE AccountTypeKey IS NULL OR LTRIM(RTRIM(AccountTypeKey)) = '';

BEGIN TRY
    ALTER TABLE dbo.Accounts ALTER COLUMN AccountTypeKey NVARCHAR(40) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Accounts_AccountTypeKey')
BEGIN
    ALTER TABLE dbo.Accounts WITH CHECK
    ADD CONSTRAINT FK_Accounts_AccountTypeKey FOREIGN KEY (AccountTypeKey) REFERENCES dbo.AccountingAccountTypes(TypeKey);
END;

IF OBJECT_ID('dbo.AccountingPostingRoles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountingPostingRoles
    (
        RoleKey NVARCHAR(80) NOT NULL PRIMARY KEY,
        Label NVARCHAR(120) NOT NULL,
        Description NVARCHAR(255) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_AccountingPostingRoles_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AccountingPostingRoles_IsActive DEFAULT (1)
    );
END;

MERGE dbo.AccountingPostingRoles AS target
USING
(
    VALUES
        ('sales_cash', 'Sales Cash', 'Debited when a sale has no customer-specific receivable account.', 10, 1),
        ('sales_revenue', 'Sales Revenue', 'Credited for the revenue side of each sale.', 20, 1),
        ('cogs', 'Cost of Goods Sold', 'Debited for the inventory cost leaving stock on a sale.', 30, 1),
        ('inventory', 'Inventory', 'Inventory control account used by both sales and purchase postings.', 40, 1),
        ('purchase_offset', 'Purchase Offset', 'Credited when a purchase invoice is posted.', 50, 1),
        ('used_car_price', 'Used Car Price', 'Default account for the vehicle purchase amount on used-car purchase posting.', 60, 1),
        ('used_car_transportation', 'Used Car Transportation', 'Default account for transportation charges on used-car purchase posting.', 70, 1),
        ('used_car_partout', 'Used Car Part-Out', 'Default account for part-out charges on used-car purchase posting.', 80, 1),
        ('used_car_shipping', 'Used Car Shipping', 'Default account for shipping charges on used-car purchase posting.', 90, 1),
        ('used_car_customs', 'Used Car Customs', 'Default account for customs charges on used-car purchase posting.', 100, 1),
        ('used_car_repairs', 'Used Car Repairs', 'Default account for repair charges on used-car purchase posting.', 110, 1)
) AS source (RoleKey, Label, Description, SortOrder, IsActive)
ON target.RoleKey = source.RoleKey
WHEN MATCHED THEN
    UPDATE SET Label = source.Label,
               Description = source.Description,
               SortOrder = source.SortOrder,
               IsActive = source.IsActive
WHEN NOT MATCHED THEN
    INSERT (RoleKey, Label, Description, SortOrder, IsActive)
    VALUES (source.RoleKey, source.Label, source.Description, source.SortOrder, source.IsActive);

IF OBJECT_ID('dbo.AccountingPostingSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AccountingPostingSettings
    (
        SettingKey NVARCHAR(80) NOT NULL PRIMARY KEY,
        AccountId INT NOT NULL,
        ModifiedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AccountingPostingSettings_ModifiedAt DEFAULT SYSUTCDATETIME(),
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_AccountingPostingSettings_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AccountingPostingSettings_Roles')
BEGIN
    ALTER TABLE dbo.AccountingPostingSettings WITH CHECK
    ADD CONSTRAINT FK_AccountingPostingSettings_Roles FOREIGN KEY (SettingKey) REFERENCES dbo.AccountingPostingRoles(RoleKey);
END;

IF OBJECT_ID('dbo.JournalEntries', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JournalEntries
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EntryDate DATETIME2(0) NOT NULL,
        ReferenceType NVARCHAR(60) NULL,
        ReferenceId INT NULL,
        Description NVARCHAR(400) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_JournalEntries_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL
    );
END;

IF OBJECT_ID('dbo.JournalLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.JournalLines
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        JournalEntryId INT NOT NULL,
        AccountId INT NOT NULL,
        Debit DECIMAL(19, 4) NOT NULL CONSTRAINT DF_JournalLines_Debit DEFAULT (0),
        Credit DECIMAL(19, 4) NOT NULL CONSTRAINT DF_JournalLines_Credit DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_JournalLines_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_JournalLines_JournalEntries FOREIGN KEY (JournalEntryId) REFERENCES dbo.JournalEntries(Id),
        CONSTRAINT FK_JournalLines_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id),
        CONSTRAINT CK_JournalLines_PositiveAmounts CHECK (Debit >= 0 AND Credit >= 0),
        CONSTRAINT CK_JournalLines_SingleSide CHECK ((CASE WHEN Debit > 0 THEN 1 ELSE 0 END) + (CASE WHEN Credit > 0 THEN 1 ELSE 0 END) = 1)
    );
END;

IF COL_LENGTH('dbo.JournalLines', 'CurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.JournalLines ADD CurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.JournalLines', 'OriginalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.JournalLines ADD OriginalAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.JournalLines', 'RateToBase') IS NULL
BEGIN
    ALTER TABLE dbo.JournalLines ADD RateToBase DECIMAL(19, 8) NULL;
END;

IF COL_LENGTH('dbo.JournalLines', 'CounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.JournalLines ADD CounterAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.JournalLines', 'BaseCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.JournalLines ADD BaseCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.JournalLines', 'CounterCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.JournalLines ADD CounterCurrencyCode CHAR(3) NULL;
END;

DECLARE @AccountingBaseCurrencyCode CHAR(3) = 'USD';
DECLARE @AccountingCounterCurrencyCode CHAR(3) = 'USD';
DECLARE @AccountingDefaultCounterRate DECIMAL(19, 8) = 1;

IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    SELECT TOP (1) @AccountingBaseCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode')
    ORDER BY CASE WHEN [Key] = 'BaseCurrencyCode' THEN 0 ELSE 1 END;

    SELECT TOP (1) @AccountingCounterCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode';

    SELECT TOP (1) @AccountingDefaultCounterRate = TRY_CONVERT(DECIMAL(19, 8), [Value])
    FROM dbo.AppConstants
    WHERE [Key] = 'DefaultCounterRate';
END;

SET @AccountingBaseCurrencyCode = COALESCE(NULLIF(@AccountingBaseCurrencyCode, ''), 'USD');
SET @AccountingCounterCurrencyCode = COALESCE(NULLIF(@AccountingCounterCurrencyCode, ''), @AccountingBaseCurrencyCode, 'USD');
SET @AccountingDefaultCounterRate = COALESCE(NULLIF(@AccountingDefaultCounterRate, 0), 1);

EXEC sp_executesql
N'
UPDATE dbo.JournalLines
SET BaseCurrencyCode = COALESCE(NULLIF(BaseCurrencyCode, ''''), @BaseCurrencyCode),
    CounterCurrencyCode = COALESCE(NULLIF(CounterCurrencyCode, ''''), @CounterCurrencyCode),
    CurrencyCode = COALESCE(NULLIF(CurrencyCode, ''''), @BaseCurrencyCode),
    OriginalAmount = CASE
        WHEN ISNULL(OriginalAmount, 0) > 0 THEN ROUND(OriginalAmount, 4)
        ELSE ROUND(CASE WHEN Debit > 0 THEN Debit ELSE Credit END, 4)
    END,
    RateToBase = CASE
        WHEN ISNULL(RateToBase, 0) > 0 THEN ROUND(RateToBase, 8)
        WHEN COALESCE(NULLIF(CurrencyCode, ''''), @BaseCurrencyCode) = @BaseCurrencyCode THEN CAST(1 AS DECIMAL(19, 8))
        WHEN COALESCE(NULLIF(CurrencyCode, ''''), @BaseCurrencyCode) = @CounterCurrencyCode THEN @CounterRateToBase
        ELSE CAST(1 AS DECIMAL(19, 8))
    END,
    CounterAmount = CASE
        WHEN ISNULL(CounterAmount, 0) > 0 THEN ROUND(CounterAmount, 4)
        WHEN COALESCE(NULLIF(CurrencyCode, ''''), @BaseCurrencyCode) = @CounterCurrencyCode THEN ROUND(
            CASE
                WHEN ISNULL(OriginalAmount, 0) > 0 THEN OriginalAmount
                ELSE CASE WHEN Debit > 0 THEN Debit ELSE Credit END
            END, 4)
        WHEN @CounterRateToBase > 0 THEN ROUND((CASE WHEN Debit > 0 THEN Debit ELSE Credit END) / @CounterRateToBase, 4)
        ELSE ROUND(CASE WHEN Debit > 0 THEN Debit ELSE Credit END, 4)
    END
WHERE BaseCurrencyCode IS NULL
   OR CounterCurrencyCode IS NULL
   OR CurrencyCode IS NULL
   OR LTRIM(RTRIM(CurrencyCode)) = ''''
   OR OriginalAmount IS NULL
   OR OriginalAmount <= 0
   OR RateToBase IS NULL
   OR RateToBase <= 0
   OR CounterAmount IS NULL
   OR CounterAmount < 0;',
N'@BaseCurrencyCode CHAR(3), @CounterCurrencyCode CHAR(3), @CounterRateToBase DECIMAL(19, 8)',
@BaseCurrencyCode = @AccountingBaseCurrencyCode,
@CounterCurrencyCode = @AccountingCounterCurrencyCode,
@CounterRateToBase = @AccountingDefaultCounterRate;

IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.UsedCarPurchaseLines', 'U') IS NOT NULL
BEGIN
    EXEC(N'
;WITH PurchaseJournalDebitLines AS
(
    SELECT jl.Id,
           je.ReferenceId AS PurchaseId,
           jl.AccountId,
           ROW_NUMBER() OVER (PARTITION BY je.ReferenceId, jl.AccountId ORDER BY jl.Id) AS RowNumber
    FROM dbo.JournalLines jl
    INNER JOIN dbo.JournalEntries je ON je.Id = jl.JournalEntryId
    WHERE je.ReferenceType = ''UsedCarPurchase''
      AND jl.Debit > 0
),
PurchaseSourceLines AS
(
    SELECT l.UsedCarPurchaseId AS PurchaseId,
           l.AccountId,
           l.CurrencyCode,
           l.Amount,
           l.RateToBase,
           l.CounterAmount,
           p.BaseCurrencyCode,
           p.CounterCurrencyCode,
           ROW_NUMBER() OVER (PARTITION BY l.UsedCarPurchaseId, l.AccountId ORDER BY l.SortOrder, l.Id) AS RowNumber
    FROM dbo.UsedCarPurchaseLines l
    INNER JOIN dbo.UsedCarPurchases p ON p.Id = l.UsedCarPurchaseId
)
UPDATE jl
SET jl.CurrencyCode = source.CurrencyCode,
    jl.OriginalAmount = ROUND(source.Amount, 4),
    jl.RateToBase = CASE WHEN source.RateToBase > 0 THEN ROUND(source.RateToBase, 8) ELSE 1 END,
    jl.CounterAmount = ROUND(source.CounterAmount, 4),
    jl.BaseCurrencyCode = source.BaseCurrencyCode,
    jl.CounterCurrencyCode = source.CounterCurrencyCode
FROM dbo.JournalLines jl
INNER JOIN PurchaseJournalDebitLines target ON target.Id = jl.Id
INNER JOIN PurchaseSourceLines source
    ON source.PurchaseId = target.PurchaseId
   AND source.AccountId = target.AccountId
   AND source.RowNumber = target.RowNumber;

UPDATE jl
SET jl.CurrencyCode = p.CounterCurrencyCode,
    jl.OriginalAmount = ROUND(CASE WHEN p.TotalCounterAmount > 0 THEN p.TotalCounterAmount ELSE p.TotalBaseAmount END, 4),
    jl.RateToBase = CASE
        WHEN p.TotalCounterAmount > 0 THEN ROUND(p.TotalBaseAmount / NULLIF(p.TotalCounterAmount, 0), 8)
        ELSE 1
    END,
    jl.CounterAmount = ROUND(CASE WHEN p.TotalCounterAmount > 0 THEN p.TotalCounterAmount ELSE p.TotalBaseAmount END, 4),
    jl.BaseCurrencyCode = p.BaseCurrencyCode,
    jl.CounterCurrencyCode = p.CounterCurrencyCode
FROM dbo.JournalLines jl
INNER JOIN dbo.JournalEntries je ON je.Id = jl.JournalEntryId
INNER JOIN dbo.UsedCarPurchases p ON p.Id = je.ReferenceId
WHERE je.ReferenceType = ''UsedCarPurchase''
  AND jl.Credit > 0;');
END;

BEGIN TRY
    ALTER TABLE dbo.JournalLines ALTER COLUMN CurrencyCode CHAR(3) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.JournalLines ALTER COLUMN OriginalAmount DECIMAL(19, 4) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.JournalLines ALTER COLUMN RateToBase DECIMAL(19, 8) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.JournalLines ALTER COLUMN CounterAmount DECIMAL(19, 4) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.JournalLines ALTER COLUMN BaseCurrencyCode CHAR(3) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.JournalLines ALTER COLUMN CounterCurrencyCode CHAR(3) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts)
BEGIN
    SET IDENTITY_INSERT dbo.Accounts ON;

    INSERT INTO dbo.Accounts (Id, Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES
        (1, '1000', 'Cash', 0, 'asset', NULL, SYSUTCDATETIME()),
        (2, '1100', 'Inventory', 0, 'asset', NULL, SYSUTCDATETIME()),
        (3, '2000', 'Accounts Payable', 1, 'liability', NULL, SYSUTCDATETIME()),
        (4, '3000', 'Owner Equity', 2, 'equity', NULL, SYSUTCDATETIME()),
        (5, '4000', 'Sales Revenue', 3, 'income', NULL, SYSUTCDATETIME()),
        (6, '5000', 'Cost of Goods Sold', 4, 'expense', NULL, SYSUTCDATETIME()),
        (7, '6000', 'Operating Expenses', 4, 'expense', NULL, SYSUTCDATETIME());

    SET IDENTITY_INSERT dbo.Accounts OFF;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '1200')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('1200', 'Customer Accounts', 0, 'asset', NULL, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '2100')
BEGIN
    DECLARE @AccountsPayableControlId INT;
    SELECT @AccountsPayableControlId = Id FROM dbo.Accounts WHERE Code = '2000';

    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('2100', 'Supplier Accounts', 1, 'liability', @AccountsPayableControlId, SYSUTCDATETIME());
END;

DECLARE @OperatingExpensesAccountId INT;
SELECT @OperatingExpensesAccountId = Id FROM dbo.Accounts WHERE Code = '6000';

IF @OperatingExpensesAccountId IS NULL
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('6000', 'Operating Expenses', 4, 'expense', NULL, SYSUTCDATETIME());

    SET @OperatingExpensesAccountId = CAST(SCOPE_IDENTITY() AS INT);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '6100')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('6100', 'Rent Expense', 4, 'expense', @OperatingExpensesAccountId, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '6200')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('6200', 'Labor Expense', 4, 'expense', @OperatingExpensesAccountId, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '1150')
BEGIN
    DECLARE @InventoryParentAccountId INT;
    SELECT @InventoryParentAccountId = Id FROM dbo.Accounts WHERE Code = '1100';

    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('1150', 'Used Car Cost', 0, 'asset', @InventoryParentAccountId, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '5210')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('5210', 'Used Car Transportation', 4, 'expense', 7, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '5220')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('5220', 'Used Car Part-Out', 4, 'expense', 7, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '5230')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('5230', 'Used Car Shipping', 4, 'expense', 7, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '5240')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('5240', 'Used Car Customs', 4, 'expense', 7, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Accounts WHERE Code = '5250')
BEGIN
    INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
    VALUES ('5250', 'Used Car Repairs', 4, 'expense', 7, SYSUTCDATETIME());
END;

DECLARE @SalesCashAccountId INT;
DECLARE @SalesRevenueAccountId INT;
DECLARE @CogsAccountId INT;
DECLARE @InventoryAccountId INT;
DECLARE @PurchaseOffsetAccountId INT;
DECLARE @UsedCarPriceAccountId INT;
DECLARE @UsedCarTransportationAccountId INT;
DECLARE @UsedCarPartOutAccountId INT;
DECLARE @UsedCarShippingAccountId INT;
DECLARE @UsedCarCustomsAccountId INT;
DECLARE @UsedCarRepairsAccountId INT;

SELECT @SalesCashAccountId = Id FROM dbo.Accounts WHERE Code = '1000';
SELECT @SalesRevenueAccountId = Id FROM dbo.Accounts WHERE Code = '4000';
SELECT @CogsAccountId = Id FROM dbo.Accounts WHERE Code = '5000';
SELECT @InventoryAccountId = Id FROM dbo.Accounts WHERE Code = '1100';
SELECT @PurchaseOffsetAccountId = Id FROM dbo.Accounts WHERE Code = '3000';
SELECT @UsedCarPriceAccountId = Id FROM dbo.Accounts WHERE Code = '1150';
SELECT @UsedCarTransportationAccountId = Id FROM dbo.Accounts WHERE Code = '5210';
SELECT @UsedCarPartOutAccountId = Id FROM dbo.Accounts WHERE Code = '5220';
SELECT @UsedCarShippingAccountId = Id FROM dbo.Accounts WHERE Code = '5230';
SELECT @UsedCarCustomsAccountId = Id FROM dbo.Accounts WHERE Code = '5240';
SELECT @UsedCarRepairsAccountId = Id FROM dbo.Accounts WHERE Code = '5250';

MERGE dbo.AccountingPostingSettings AS target
USING
(
    VALUES
        ('sales_cash', @SalesCashAccountId),
        ('sales_revenue', @SalesRevenueAccountId),
        ('cogs', @CogsAccountId),
        ('inventory', @InventoryAccountId),
        ('purchase_offset', @PurchaseOffsetAccountId),
        ('used_car_price', @UsedCarPriceAccountId),
        ('used_car_transportation', @UsedCarTransportationAccountId),
        ('used_car_partout', @UsedCarPartOutAccountId),
        ('used_car_shipping', @UsedCarShippingAccountId),
        ('used_car_customs', @UsedCarCustomsAccountId),
        ('used_car_repairs', @UsedCarRepairsAccountId)
) AS source (SettingKey, AccountId)
ON target.SettingKey = source.SettingKey
WHEN MATCHED AND target.AccountId IS NULL AND source.AccountId IS NOT NULL THEN
    UPDATE SET AccountId = source.AccountId, ModifiedAt = SYSUTCDATETIME()
WHEN NOT MATCHED AND source.AccountId IS NOT NULL THEN
    INSERT (SettingKey, AccountId, ModifiedAt)
    VALUES (source.SettingKey, source.AccountId, SYSUTCDATETIME());

IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Customers', 'AccountId') IS NULL
    BEGIN
        ALTER TABLE dbo.Customers ADD AccountId INT NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Customers_Accounts')
    BEGIN
        ALTER TABLE dbo.Customers WITH CHECK
        ADD CONSTRAINT FK_Customers_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Customers_AccountId' AND object_id = OBJECT_ID('dbo.Customers'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_Customers_AccountId
            ON dbo.Customers(AccountId)
            WHERE AccountId IS NOT NULL;
    END;

    DECLARE @CustomerParentAccountId INT;
    SELECT @CustomerParentAccountId = Id FROM dbo.Accounts WHERE Code = '1200';

    DECLARE @CustomerId INT;
    DECLARE @CustomerName NVARCHAR(255);
    DECLARE @CustomerAccountId INT;
    DECLARE @CustomerAccountCode NVARCHAR(20);

    DECLARE customer_cursor CURSOR FAST_FORWARD FOR
        SELECT Id, Name
        FROM dbo.Customers
        WHERE ISNULL(AccountId, 0) <= 0
        ORDER BY Id;

    OPEN customer_cursor;
    FETCH NEXT FROM customer_cursor INTO @CustomerId, @CustomerName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @CustomerAccountId = NULL;
        SET @CustomerAccountCode = CONCAT('CUST-', RIGHT(CONCAT('000000', CONVERT(NVARCHAR(10), @CustomerId)), 6));

        SELECT @CustomerAccountId = Id
        FROM dbo.Accounts
        WHERE Code = @CustomerAccountCode;

        IF @CustomerAccountId IS NULL
        BEGIN
            INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
            VALUES
            (
                @CustomerAccountCode,
                LEFT(CONCAT('Customer - ', ISNULL(@CustomerName, CONCAT('Customer ', @CustomerId))), 160),
                0,
                'asset',
                @CustomerParentAccountId,
                SYSUTCDATETIME()
            );

            SET @CustomerAccountId = CAST(SCOPE_IDENTITY() AS INT);
        END;

        UPDATE dbo.Customers
        SET AccountId = @CustomerAccountId
        WHERE Id = @CustomerId;

        FETCH NEXT FROM customer_cursor INTO @CustomerId, @CustomerName;
    END;

    CLOSE customer_cursor;
    DEALLOCATE customer_cursor;

    UPDATE a
    SET a.Name = LEFT(CONCAT('Customer - ', c.Name), 160),
        a.AccountTypeKey = 'asset',
        a.ParentId = @CustomerParentAccountId
    FROM dbo.Accounts a
    INNER JOIN dbo.Customers c ON c.AccountId = a.Id;

END;

IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Suppliers', 'AccountId') IS NULL
    BEGIN
        ALTER TABLE dbo.Suppliers ADD AccountId INT NULL;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Suppliers_Accounts')
    BEGIN
        ALTER TABLE dbo.Suppliers WITH CHECK
        ADD CONSTRAINT FK_Suppliers_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Suppliers_AccountId' AND object_id = OBJECT_ID('dbo.Suppliers'))
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_Suppliers_AccountId
            ON dbo.Suppliers(AccountId)
            WHERE AccountId IS NOT NULL;
    END;

    DECLARE @SupplierParentAccountId INT;
    SELECT @SupplierParentAccountId = Id FROM dbo.Accounts WHERE Code = '2100';

    DECLARE @SupplierId INT;
    DECLARE @SupplierName NVARCHAR(255);
    DECLARE @SupplierAccountId INT;
    DECLARE @SupplierAccountCode NVARCHAR(20);

    DECLARE supplier_cursor CURSOR FAST_FORWARD FOR
        SELECT Id, Name
        FROM dbo.Suppliers
        WHERE ISNULL(AccountId, 0) <= 0
        ORDER BY Id;

    OPEN supplier_cursor;
    FETCH NEXT FROM supplier_cursor INTO @SupplierId, @SupplierName;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @SupplierAccountId = NULL;
        SET @SupplierAccountCode = CONCAT('SUP-', RIGHT(CONCAT('000000', CONVERT(NVARCHAR(10), @SupplierId)), 6));

        SELECT @SupplierAccountId = Id
        FROM dbo.Accounts
        WHERE Code = @SupplierAccountCode;

        IF @SupplierAccountId IS NULL
        BEGIN
            INSERT INTO dbo.Accounts (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt)
            VALUES
            (
                @SupplierAccountCode,
                LEFT(CONCAT('Supplier - ', ISNULL(@SupplierName, CONCAT('Supplier ', @SupplierId))), 160),
                1,
                'liability',
                @SupplierParentAccountId,
                SYSUTCDATETIME()
            );

            SET @SupplierAccountId = CAST(SCOPE_IDENTITY() AS INT);
        END;

        UPDATE dbo.Suppliers
        SET AccountId = @SupplierAccountId
        WHERE Id = @SupplierId;

        FETCH NEXT FROM supplier_cursor INTO @SupplierId, @SupplierName;
    END;

    CLOSE supplier_cursor;
    DEALLOCATE supplier_cursor;

    UPDATE a
    SET a.Name = LEFT(CONCAT('Supplier - ', s.Name), 160),
        a.AccountTypeKey = 'liability',
        a.ParentId = @SupplierParentAccountId
    FROM dbo.Accounts a
    INNER JOIN dbo.Suppliers s ON s.AccountId = a.Id;
END;
GO

-- ── WebAppUserRoleMigration ───────────────────────────────────────────────
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 4)
    BEGIN
        UPDATE dbo.Roles
        SET Name = N'Web App User',
            Description = COALESCE(NULLIF(Description, N''), N'Customer web/mobile shopping role'),
            BadgeColor = COALESCE(NULLIF(BadgeColor, N''), N'#2225D366'),
            BadgeTextColor = COALESCE(NULLIF(BadgeTextColor, N''), N'#FFFFFF'),
            IsSystem = 1,
            IsActive = 1,
            ModifiedAt = SYSUTCDATETIME()
        WHERE Id = 4;
    END
    ELSE
    BEGIN
        SET IDENTITY_INSERT dbo.Roles ON;

        INSERT INTO dbo.Roles
            (Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive, CreatedAt)
        VALUES
            (4, N'Web App User', N'Customer web/mobile shopping role', N'#2225D366', N'#FFFFFF', 1, 1, SYSUTCDATETIME());

        SET IDENTITY_INSERT dbo.Roles OFF;
    END
END;
GO

-- ── UserRoleIdMigration ───────────────────────────────────────────────────
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    DECLARE @SystemRoles TABLE
    (
        Id INT NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        BadgeColor NVARCHAR(20) NOT NULL,
        BadgeTextColor NVARCHAR(20) NOT NULL
    );

    INSERT INTO @SystemRoles (Id, Name, Description, BadgeColor, BadgeTextColor)
    VALUES
        (1, N'Admin', N'Full system access', N'#22FF5722', N'#FF7043'),
        (2, N'Manager', N'Operations access, no user management', N'#2200E5FF', N'#00E5FF'),
        (3, N'Cashier', N'POS sales only', N'#2244FF44', N'#44FF44'),
        (4, N'Web App User', N'Customer web/mobile shopping role', N'#2225D366', N'#FFFFFF');

    UPDATE r
    SET Name = s.Name,
        Description = COALESCE(NULLIF(r.Description, N''), s.Description),
        BadgeColor = COALESCE(NULLIF(r.BadgeColor, N''), s.BadgeColor),
        BadgeTextColor = COALESCE(NULLIF(r.BadgeTextColor, N''), s.BadgeTextColor),
        IsSystem = 1,
        IsActive = 1,
        ModifiedAt = SYSUTCDATETIME()
    FROM dbo.Roles r
    INNER JOIN @SystemRoles s ON s.Id = r.Id;

    IF EXISTS (SELECT 1 FROM @SystemRoles s WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = s.Id))
    BEGIN
        SET IDENTITY_INSERT dbo.Roles ON;

        INSERT INTO dbo.Roles
            (Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive, CreatedAt)
        SELECT s.Id, s.Name, s.Description, s.BadgeColor, s.BadgeTextColor, 1, 1, SYSUTCDATETIME()
        FROM @SystemRoles s
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = s.Id);

        SET IDENTITY_INSERT dbo.Roles OFF;
    END
END;

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Users', 'RoleId') IS NULL
    BEGIN
        ALTER TABLE dbo.Users ADD RoleId INT NULL;
    END;

    IF COL_LENGTH('dbo.Users', 'Role') IS NOT NULL
    BEGIN
        EXEC(N'
UPDATE u
SET RoleId = r.Id
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.Name = CONVERT(NVARCHAR(100), u.Role)
WHERE u.RoleId IS NULL;');
    END;

    UPDATE u
    SET RoleId = 3
    FROM dbo.Users u
    WHERE u.RoleId IS NULL
       OR NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = u.RoleId);

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE RoleId IS NULL)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Users')
              AND name = N'RoleId'
              AND is_nullable = 1
        )
        BEGIN
            ALTER TABLE dbo.Users ALTER COLUMN RoleId INT NOT NULL;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
              AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Users'), N'RoleId', 'ColumnId')
        )
        BEGIN
            ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_RoleId DEFAULT (3) FOR RoleId;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
              AND name = N'FK_Users_Roles_RoleId'
        )
        BEGIN
            ALTER TABLE dbo.Users WITH CHECK
            ADD CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id);
        END;
    END;

    IF COL_LENGTH('dbo.Users', 'Role') IS NOT NULL
    BEGIN
        DECLARE @RoleDefaultConstraint SYSNAME;
        DECLARE @DropRoleDefaultSql NVARCHAR(MAX);

        WHILE 1 = 1
        BEGIN
            SELECT TOP (1) @RoleDefaultConstraint = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
              AND c.name = N'Role';

            IF @RoleDefaultConstraint IS NULL
            BEGIN
                BREAK;
            END;

            SET @DropRoleDefaultSql = N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(@RoleDefaultConstraint);
            EXEC sp_executesql @DropRoleDefaultSql;
            SET @RoleDefaultConstraint = NULL;
        END;

        ALTER TABLE dbo.Users DROP COLUMN Role;
    END;
END;
GO

-- ── MenuAccessMigration ───────────────────────────────────────────────────
IF OBJECT_ID('dbo.AppMenus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppMenus
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MenuKey NVARCHAR(100) NOT NULL UNIQUE,
        MenuName NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_AppMenus_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AppMenus_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AppMenus_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.RoleMenuAccess', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMenuAccess
    (
        RoleId INT NOT NULL,
        MenuId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_RMA_CanView DEFAULT (0),
        CanEdit BIT NOT NULL CONSTRAINT DF_RMA_CanEdit DEFAULT (0),
        CanModify BIT NOT NULL CONSTRAINT DF_RMA_CanModify DEFAULT (0),
        CanDelete BIT NOT NULL CONSTRAINT DF_RMA_CanDelete DEFAULT (0),
        ModifiedAt DATETIME2 NULL,
        CONSTRAINT PK_RoleMenuAccess PRIMARY KEY (RoleId, MenuId),
        CONSTRAINT FK_RoleMenuAccess_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_RoleMenuAccess_Menus FOREIGN KEY (MenuId) REFERENCES dbo.AppMenus(Id) ON DELETE CASCADE
    );
END;

MERGE dbo.AppMenus AS target
USING (VALUES
    ('home_screen', 'Home Screen', 5),
    ('pos_screen', 'POS Screen', 8),
    ('car_selection_screen', 'Car Selection Screen', 9),
    ('part_selection_screen', 'Part Selection Screen', 9),
    ('invoice_create', 'Create Invoice', 10),
    ('invoice_search', 'Invoice Search', 11),
    ('purchases_screen', 'Purchases Screen', 15),
    ('stock_management_screen', 'Stock Management Screen', 18),
    ('accounting_screen', 'Accounting Screen', 19),
    ('manual_journal_screen', 'Manual Journal Screen', 20),
    ('management_screen', 'Management Screen', 21),
    ('report_builder_screen', 'Report Builder', 22),
    ('barcode_qr_screen', 'Barcode / QR Mode', 23),
    ('whatsapp_screen', 'WhatsApp Conversations', 24),
    ('web_catalog', 'Web Catalog', 25),
    ('web_checkout', 'Web Checkout', 26),
    ('supplier_tab', 'Supplier Tab', 30),
    ('currency_tab', 'Currency Tab', 31),
    ('transaction_types_tab', 'Transaction Types Tab', 32)
) AS source(MenuKey, MenuName, SortOrder)
ON target.MenuKey = source.MenuKey
WHEN MATCHED THEN
    UPDATE SET MenuName = source.MenuName, SortOrder = source.SortOrder, IsActive = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (MenuKey, MenuName, SortOrder, IsActive) VALUES (source.MenuKey, source.MenuName, source.SortOrder, 1);

INSERT INTO dbo.RoleMenuAccess (RoleId, MenuId, CanView, CanEdit, CanModify, CanDelete)
SELECT r.Id,
       m.Id,
       CASE 
         WHEN m.MenuKey IN ('home_screen','pos_screen','car_selection_screen','part_selection_screen') AND r.Id IN (1,2,3) THEN 1
         WHEN m.MenuKey IN ('invoice_create','invoice_search') AND r.Id IN (1,2,3) THEN 1
         WHEN m.MenuKey IN ('whatsapp_screen','barcode_qr_screen') AND r.Id IN (1,2,3) THEN 1
         WHEN m.MenuKey IN ('web_catalog','web_checkout') AND r.Id = 4 THEN 1
         WHEN m.MenuKey IN ('purchases_screen','stock_management_screen','accounting_screen','manual_journal_screen','report_builder_screen') AND r.Id IN (1,2) THEN 1
         WHEN m.MenuKey IN ('management_screen','supplier_tab','currency_tab','transaction_types_tab') AND r.Id IN (1,2) THEN 1
         ELSE 0
       END AS CanView,
       CASE
         WHEN m.MenuKey = 'invoice_create' AND r.Id IN (1,2,3) THEN 1
         WHEN m.MenuKey = 'supplier_tab' AND r.Id IN (1,2) THEN 1
         ELSE 0
       END AS CanEdit,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Id IN (1,2) THEN 1 ELSE 0 END AS CanModify,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Id = 1 THEN 1 ELSE 0 END AS CanDelete
FROM dbo.Roles r
CROSS JOIN dbo.AppMenus m
WHERE m.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenuAccess a WHERE a.RoleId = r.Id AND a.MenuId = m.Id);
GO

-- ── TransactionTypesMigration ─────────────────────────────────────────────
IF OBJECT_ID('dbo.TransactionTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionTypes
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL UNIQUE,
        CurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_TransactionTypes_Currency DEFAULT ('USD'),
        CounterRate DECIMAL(19, 8) NOT NULL CONSTRAINT CK_TransactionTypes_CounterRate_Positive CHECK (CounterRate > 0),
        IsActive BIT NOT NULL CONSTRAINT DF_TransactionTypes_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TransactionTypes_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes)
BEGIN
    INSERT INTO dbo.TransactionTypes (Name, CurrencyCode, CounterRate, IsActive)
    VALUES
        ('Sales', 'USD', 1, 1),
        ('Purchases', 'USD', 1, 1);
END;
GO

-- ── PartAveragePriceMigration ─────────────────────────────────────────────
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Parts', 'AveragePrice') IS NULL
            BEGIN
                ALTER TABLE dbo.Parts
                ADD AveragePrice DECIMAL(19, 4) NULL;
            END;
GO

-- ── PartUsedCarMigration ──────────────────────────────────────────────────
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NULL
BEGIN
    ALTER TABLE dbo.Parts
        ADD UsedCarId INT NULL;
END;

IF OBJECT_ID('dbo.UsedCarParts', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    ;WITH RankedUsedCarParts AS
    (
        SELECT PartId,
               UsedCarId,
               ROW_NUMBER() OVER (PARTITION BY PartId ORDER BY UsedCarId) AS RowNumber
        FROM dbo.UsedCarParts
    )
    UPDATE p
    SET UsedCarId = ranked.UsedCarId
    FROM dbo.Parts p
    INNER JOIN RankedUsedCarParts ranked
        ON ranked.PartId = p.Id
       AND ranked.RowNumber = 1
    WHERE p.UsedCarId IS NULL;

    DROP TABLE dbo.UsedCarParts;
END;

IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Parts_UsedCars'
          AND parent_object_id = OBJECT_ID('dbo.Parts'))
       AND OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Parts WITH NOCHECK
            ADD CONSTRAINT FK_Parts_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars (Id);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Parts_UsedCarId'
          AND object_id = OBJECT_ID('dbo.Parts'))
    BEGIN
        CREATE INDEX IX_Parts_UsedCarId ON dbo.Parts (UsedCarId);
    END;
END;
GO

-- ── CurrencyRatesMigration ────────────────────────────────────────────────
IF OBJECT_ID('dbo.CurrencyRates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CurrencyRates
    (
        Code CHAR(3) NOT NULL PRIMARY KEY,
        RateToUsd DECIMAL(19, 8) NOT NULL CONSTRAINT CK_CurrencyRates_PositiveRate CHECK (RateToUsd > 0),
        BaseCode CHAR(3) NOT NULL CONSTRAINT DF_CurrencyRates_BaseCode DEFAULT ('USD'),
        SnapshotUtc DATETIME2(0) NOT NULL CONSTRAINT DF_CurrencyRates_SnapshotUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO

-- ── AppConstantsMigration ─────────────────────────────────────────────────
IF OBJECT_ID('dbo.AppConstants', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppConstants
    (
        [Key] NVARCHAR(120) NOT NULL PRIMARY KEY,
        [Value] NVARCHAR(MAX) NOT NULL,
        Description NVARCHAR(250) NULL,
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppConstants_UpdatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF COL_LENGTH('dbo.AppConstants', 'Value') <> -1
BEGIN
    ALTER TABLE dbo.AppConstants
    ALTER COLUMN [Value] NVARCHAR(MAX) NOT NULL;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'BrandRegionOrder')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('BrandRegionOrder', 'German,Japanese,Korean', 'Display order for car brand region groups.');
END;


IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'BaseCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('BaseCurrencyCode', 'USD', 'Application base currency code used for totals and conversions.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'CounterCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('CounterCurrencyCode', 'USD', 'Application counter/default transaction currency code.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DisplayCurrencyCode')
BEGIN
    DECLARE @DisplayCurrencyCode NVARCHAR(3);

    SELECT TOP (1) @DisplayCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode'
      AND LEN(LTRIM(RTRIM([Value]))) = 3;

    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DisplayCurrencyCode', COALESCE(@DisplayCurrencyCode, 'USD'), 'Application display currency code used by screens for money totals.');
END;
IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DefaultCurrencyCode')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DefaultCurrencyCode', 'USD', 'Fallback invoice currency code.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DefaultCounterRate')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DefaultCounterRate', '1', 'Fallback counter/base rate when no currency mapping exists.');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = 'DefaultSalesTransactionTypeName')
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES ('DefaultSalesTransactionTypeName', 'Sales', 'Default transaction type used when creating invoices.');
END;
GO

-- ── CarModelsMigration ────────────────────────────────────────────────────
IF OBJECT_ID('dbo.CarModels', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.CarModels', 'BodyType') IS NULL
BEGIN
    ALTER TABLE dbo.CarModels ADD BodyType NVARCHAR(60) NULL;
END;
GO

-- ── LocationsMigration ────────────────────────────────────────────────────
DECLARE @DefaultLocationCurrencyCode CHAR(3) = NULL;

IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    SELECT TOP (1) @DefaultLocationCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] = 'CounterCurrencyCode'
      AND LEN(LTRIM(RTRIM([Value]))) = 3;

    IF @DefaultLocationCurrencyCode IS NULL
    BEGIN
        SELECT TOP (1) @DefaultLocationCurrencyCode = UPPER(LTRIM(RTRIM([Value])))
        FROM dbo.AppConstants
        WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode')
          AND LEN(LTRIM(RTRIM([Value]))) = 3
        ORDER BY CASE WHEN [Key] = 'BaseCurrencyCode' THEN 0 ELSE 1 END;
    END;
END;

IF @DefaultLocationCurrencyCode IS NULL
BEGIN
    SET @DefaultLocationCurrencyCode = 'USD';
END;

IF OBJECT_ID('dbo.Location', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Location
    (
        LocationID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Location PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        ShippingFees DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Location_ShippingFees DEFAULT (0),
        ShippingFeesCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_Location_ShippingFeesCurrencyCode DEFAULT ('USD'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Location_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Location_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id)
    );
END;

IF COL_LENGTH('dbo.Location', 'Name') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD Name NVARCHAR(160) NULL;

    IF COL_LENGTH('dbo.Location', 'Description') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET Name = NULLIF(LTRIM(RTRIM(CAST([Description] AS NVARCHAR(160)))), N'''')
              WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'''';');
    END;

    IF COL_LENGTH('dbo.Location', 'Code') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET Name = NULLIF(LTRIM(RTRIM(CAST([Code] AS NVARCHAR(160)))), N'''')
              WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'''';');
    END;

    UPDATE dbo.Location
    SET Name = N'Location ' + CAST(LocationID AS NVARCHAR(20))
    WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'';

    ALTER TABLE dbo.Location ALTER COLUMN Name NVARCHAR(160) NOT NULL;
END;

IF COL_LENGTH('dbo.Location', 'ShippingFees') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ShippingFees DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Location_ShippingFees DEFAULT (0);

    IF COL_LENGTH('dbo.Location', 'Shipping') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET ShippingFees = ISNULL(TRY_CONVERT(DECIMAL(18, 2), [Shipping]), 0);');
    END;
END;

IF COL_LENGTH('dbo.Location', 'ShippingFeesCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ShippingFeesCurrencyCode CHAR(3) NULL;

    IF COL_LENGTH('dbo.Location', 'ShippingCurrencyCode') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET ShippingFeesCurrencyCode = UPPER(LEFT(LTRIM(RTRIM(CAST([ShippingCurrencyCode] AS NVARCHAR(16)))), 3))
              WHERE ShippingFeesCurrencyCode IS NULL
                 OR LTRIM(RTRIM(ShippingFeesCurrencyCode)) = N'''';');
    END;

    IF COL_LENGTH('dbo.Location', 'CurrencyCode') IS NOT NULL
    BEGIN
        EXEC('UPDATE dbo.Location
              SET ShippingFeesCurrencyCode = UPPER(LEFT(LTRIM(RTRIM(CAST([CurrencyCode] AS NVARCHAR(16)))), 3))
              WHERE ShippingFeesCurrencyCode IS NULL
                 OR LTRIM(RTRIM(ShippingFeesCurrencyCode)) = N'''';');
    END;
END;

UPDATE dbo.Location
SET ShippingFeesCurrencyCode = @DefaultLocationCurrencyCode
WHERE ShippingFeesCurrencyCode IS NULL
   OR LEN(LTRIM(RTRIM(ShippingFeesCurrencyCode))) <> 3;

IF COL_LENGTH('dbo.Location', 'ShippingFeesCurrencyCode') IS NOT NULL
BEGIN
    -- Drop any DEFAULT constraint on this column before altering it (name may be auto-generated)
    DECLARE @dfName NVARCHAR(256);
    SELECT @dfName = dc.name
    FROM   sys.default_constraints dc
    JOIN   sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
    WHERE  OBJECT_NAME(dc.parent_object_id) = 'Location' AND c.name = 'ShippingFeesCurrencyCode';
    IF @dfName IS NOT NULL
        EXEC('ALTER TABLE dbo.Location DROP CONSTRAINT [' + @dfName + ']');

    ALTER TABLE dbo.Location ALTER COLUMN ShippingFeesCurrencyCode CHAR(3) NOT NULL;

    -- Re-add a named DEFAULT
    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints dc
        JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
        WHERE OBJECT_NAME(dc.parent_object_id) = 'Location' AND c.name = 'ShippingFeesCurrencyCode'
    )
        ALTER TABLE dbo.Location ADD CONSTRAINT DF_Location_ShippingFeesCurrencyCode DEFAULT 'USD' FOR ShippingFeesCurrencyCode;
END;

IF COL_LENGTH('dbo.Location', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSUTCDATETIME();
END;

IF COL_LENGTH('dbo.Location', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD CreatedByUserId INT NULL;
    ALTER TABLE dbo.Location WITH NOCHECK
        ADD CONSTRAINT FK_Location_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id);
END;

IF COL_LENGTH('dbo.Location', 'ModifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ModifiedAt DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.Location', 'ModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Location ADD ModifiedByUserId INT NULL;
    ALTER TABLE dbo.Location WITH NOCHECK
        ADD CONSTRAINT FK_Location_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Location_Name'
      AND object_id = OBJECT_ID('dbo.Location'))
BEGIN
    CREATE INDEX IX_Location_Name ON dbo.Location (Name);
END;
GO

-- ── UsedCarsMigration ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCars', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCars
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCars PRIMARY KEY,
        SupplierId INT NULL,
        CarModelId INT NOT NULL,
        ModelYear INT NOT NULL,
        PriceCurrency CHAR(3) NOT NULL,
        Price DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_Price_Positive CHECK (Price > 0),
        PriceBase DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_PriceBase_NonNegative CHECK (PriceBase >= 0),
        PriceCounter DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_PriceCounter_NonNegative CHECK (PriceCounter >= 0),
        LocationId INT NULL,
        Location NVARCHAR(160) NOT NULL CONSTRAINT DF_UsedCars_Location DEFAULT (N''),
        Transportation DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Transportation DEFAULT (0),
        IsReceived BIT NOT NULL CONSTRAINT DF_UsedCars_IsReceived DEFAULT (0),
        IsShipped BIT NOT NULL CONSTRAINT DF_UsedCars_IsShipped DEFAULT (0),
        PartOutAmount DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_PartOutAmount DEFAULT (0),
        Shipping DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Shipping DEFAULT (0),
        Customs DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Customs DEFAULT (0),
        Repairs DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Repairs DEFAULT (0),
        TotalBeforeShipping DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_TotalBeforeShipping DEFAULT (0),
        GrandTotalBase DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_GrandTotalBase DEFAULT (0),
        GrandTotalCounter DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_GrandTotalCounter DEFAULT (0),
        BaseCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_UsedCars_BaseCurrencyCode DEFAULT ('USD'),
        CounterCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_UsedCars_CounterCurrencyCode DEFAULT ('USD'),
        CounterRateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCars_CounterRateToBase DEFAULT (1),
        ReceivedAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCars_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt DATETIME2(0) NULL,
        CreatedByUserId INT NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCars_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id),
        CONSTRAINT FK_UsedCars_CarModels FOREIGN KEY (CarModelId) REFERENCES dbo.CarModels (Id),
        CONSTRAINT FK_UsedCars_Location FOREIGN KEY (LocationId) REFERENCES dbo.Location (LocationId),
        CONSTRAINT FK_UsedCars_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_UsedCars_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_UsedCars_SupplierId ON dbo.UsedCars (SupplierId);
    CREATE INDEX IX_UsedCars_CarModelId ON dbo.UsedCars (CarModelId);
    CREATE INDEX IX_UsedCars_LocationId ON dbo.UsedCars (LocationId);
END;

IF COL_LENGTH('dbo.UsedCars', 'SupplierId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD SupplierId INT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'LocationId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD LocationId INT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'IsReceived') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD IsReceived BIT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'IsShipped') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD IsShipped BIT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'PartOutAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD PartOutAmount DECIMAL(18, 2) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'BaseCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD BaseCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'CounterCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD CounterCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'CounterRateToBase') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD CounterRateToBase DECIMAL(19, 8) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'ReceivedAt') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD ReceivedAt DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'Repairs') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD Repairs DECIMAL(18, 2) NULL;
END;
GO

-- ── UsedCarPartPricingMigration ───────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCars', 'ExpectedSellThroughRate') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars
        ADD ExpectedSellThroughRate DECIMAL(5, 4) NOT NULL
            CONSTRAINT DF_UsedCars_ExpectedSellThroughRate DEFAULT (0.8000);
END;

IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
BEGIN
    UPDATE dbo.UsedCars
    SET ExpectedSellThroughRate = 0.8000
    WHERE ExpectedSellThroughRate IS NULL
       OR ExpectedSellThroughRate <= 0
       OR ExpectedSellThroughRate > 1;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_UsedCars_ExpectedSellThroughRate'
          AND parent_object_id = OBJECT_ID('dbo.UsedCars'))
    BEGIN
        ALTER TABLE dbo.UsedCars WITH NOCHECK
            ADD CONSTRAINT CK_UsedCars_ExpectedSellThroughRate
            CHECK (ExpectedSellThroughRate > 0 AND ExpectedSellThroughRate <= 1);
    END;
END;
GO

-- ── UsedCarPurchasesMigration ─────────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarPurchases
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarPurchases PRIMARY KEY,
        PurchaseNumber NVARCHAR(32) NOT NULL,
        UsedCarId INT NOT NULL,
        SupplierId INT NOT NULL,
        PurchaseDate DATETIME2(0) NOT NULL,
        BaseCurrencyCode CHAR(3) NOT NULL,
        CounterCurrencyCode CHAR(3) NOT NULL,
        TotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_TotalBaseAmount DEFAULT (0),
        TotalCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_TotalCounterAmount DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaidAmount DEFAULT (0),
        PaidCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaidCounterAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarPurchases_PaymentStatus DEFAULT (N'Unpaid'),
        PostingStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarPurchases_PostingStatus DEFAULT (N'Draft'),
        PostedAt DATETIME2(0) NULL,
        PostedByUserId INT NULL,
        Notes NVARCHAR(400) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarPurchases_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarPurchases_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars(Id),
        CONSTRAINT FK_UsedCarPurchases_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers(Id)
    );
END;

IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCarPurchases', 'PurchaseNumber') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCarPurchases', 'PurchaseNumber') < 64
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ALTER COLUMN PurchaseNumber NVARCHAR(32) NOT NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'CounterCurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD CounterCurrencyCode CHAR(3) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'TotalCounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD TotalCounterAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PaidCounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PaidCounterAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PostingStatus') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PostingStatus NVARCHAR(20) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PostedAt') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PostedAt DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchases', 'PostedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchases ADD PostedByUserId INT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UsedCarPurchases_PurchaseNumber'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
BEGIN
    CREATE UNIQUE INDEX UX_UsedCarPurchases_PurchaseNumber
        ON dbo.UsedCarPurchases(PurchaseNumber);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarPurchases_UsedCarId'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
BEGIN
    CREATE INDEX IX_UsedCarPurchases_UsedCarId
        ON dbo.UsedCarPurchases(UsedCarId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarPurchases_SupplierId'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
BEGIN
    CREATE INDEX IX_UsedCarPurchases_SupplierId
        ON dbo.UsedCarPurchases(SupplierId);
END;

IF OBJECT_ID('dbo.UsedCarPurchaseLines', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarPurchaseLines
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarPurchaseLines PRIMARY KEY,
        UsedCarPurchaseId INT NOT NULL,
        DetailKey NVARCHAR(80) NOT NULL,
        Description NVARCHAR(160) NOT NULL,
        Amount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_Amount DEFAULT (0),
        CurrencyCode CHAR(3) NOT NULL,
        RateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_RateToBase DEFAULT (1),
        BaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_BaseAmount DEFAULT (0),
        CounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_CounterAmount DEFAULT (0),
        AccountId INT NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_SortOrder DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarPurchaseLines_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarPurchaseLines_UsedCarPurchases FOREIGN KEY (UsedCarPurchaseId) REFERENCES dbo.UsedCarPurchases(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UsedCarPurchaseLines_Accounts FOREIGN KEY (AccountId) REFERENCES dbo.Accounts(Id)
    );
END;

IF COL_LENGTH('dbo.UsedCarPurchaseLines', 'CounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchaseLines ADD CounterAmount DECIMAL(19, 4) NULL;
END;

IF COL_LENGTH('dbo.UsedCarPurchaseLines', 'DetailKey') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarPurchaseLines ADD DetailKey NVARCHAR(80) NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarPurchaseLines_UsedCarPurchaseId'
      AND object_id = OBJECT_ID('dbo.UsedCarPurchaseLines'))
BEGIN
    CREATE INDEX IX_UsedCarPurchaseLines_UsedCarPurchaseId
        ON dbo.UsedCarPurchaseLines(UsedCarPurchaseId, SortOrder, Id);
END;
GO

-- ── UsedCarWholesaleSalesMigration ────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCarWholesaleSaleNumberSequence', 'SO') IS NULL
BEGIN
    CREATE SEQUENCE dbo.UsedCarWholesaleSaleNumberSequence
        AS INT
        START WITH 1
        INCREMENT BY 1;
END;

IF OBJECT_ID('dbo.UsedCarWholesaleSales', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarWholesaleSales
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarWholesaleSales PRIMARY KEY,
        SaleNumber NVARCHAR(32) NOT NULL,
        UsedCarId INT NOT NULL,
        CustomerId INT NULL,
        BuyerName NVARCHAR(160) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_BuyerName DEFAULT (N''),
        BuyerPhone NVARCHAR(60) NULL,
        SaleDate DATETIME2(0) NOT NULL,
        CurrencyCode CHAR(3) NOT NULL,
        SalePrice DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SalePrice DEFAULT (0),
        SaleRateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SaleRateToBase DEFAULT (1),
        SalePriceBase DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SalePriceBase DEFAULT (0),
        CounterCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_CounterCurrencyCode DEFAULT ('USD'),
        CounterRateToBase DECIMAL(19, 8) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_CounterRateToBase DEFAULT (1),
        SalePriceCounter DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SalePriceCounter DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaidAmount DEFAULT (0),
        PaidBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaidBaseAmount DEFAULT (0),
        PaidCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaidCounterAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_PaymentStatus DEFAULT (N'Unpaid'),
        IsForParts BIT NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_IsForParts DEFAULT (0),
        RepairItemsJson NVARCHAR(MAX) NULL,
        RepairTotalAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalAmount DEFAULT (0),
        RepairTotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalBaseAmount DEFAULT (0),
        RepairTotalCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalCounterAmount DEFAULT (0),
        PaymentMethod NVARCHAR(80) NULL,
        Notes NVARCHAR(800) NULL,
        SoldAsIsAcknowledged BIT NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_SoldAsIsAcknowledged DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCarWholesaleSales_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars(Id)
    );
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'IsForParts') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD IsForParts BIT NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_IsForParts DEFAULT (0);
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairItemsJson') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairItemsJson NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairTotalAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairTotalAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalAmount DEFAULT (0);
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairTotalBaseAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairTotalBaseAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalBaseAmount DEFAULT (0);
END;

IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'RepairTotalCounterAmount') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales
        ADD RepairTotalCounterAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_UsedCarWholesaleSales_RepairTotalCounterAmount DEFAULT (0);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UsedCarWholesaleSales_SaleNumber'
      AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    CREATE UNIQUE INDEX UX_UsedCarWholesaleSales_SaleNumber
        ON dbo.UsedCarWholesaleSales(SaleNumber);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_UsedCarWholesaleSales_UsedCarId'
      AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    CREATE UNIQUE INDEX UX_UsedCarWholesaleSales_UsedCarId
        ON dbo.UsedCarWholesaleSales(UsedCarId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCarWholesaleSales_CustomerId'
      AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    CREATE INDEX IX_UsedCarWholesaleSales_CustomerId
        ON dbo.UsedCarWholesaleSales(CustomerId);
END;

IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_UsedCarWholesaleSales_Customers'
         AND parent_object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales WITH NOCHECK
        ADD CONSTRAINT FK_UsedCarWholesaleSales_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id);
END;

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.foreign_keys
       WHERE name = 'FK_UsedCarWholesaleSales_CreatedByUsers'
         AND parent_object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
BEGIN
    ALTER TABLE dbo.UsedCarWholesaleSales WITH NOCHECK
        ADD CONSTRAINT FK_UsedCarWholesaleSales_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users(Id);
END;
GO

-- ── TransactionsMigration ─────────────────────────────────────────────────
IF COL_LENGTH('dbo.TransactionTypes', 'TypeKey') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD TypeKey NVARCHAR(80) NULL;
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SortOrder') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SortOrder INT NOT NULL CONSTRAINT DF_TransactionTypes_SortOrder DEFAULT (0);
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SerialNumberFormat') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SerialNumberFormat NVARCHAR(200) NULL;
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SerialStartNumber') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SerialStartNumber BIGINT NULL;
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SerialCurrentNumber') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SerialCurrentNumber BIGINT NULL;
END;

UPDATE dbo.TransactionTypes
SET TypeKey = CASE
        WHEN UPPER(LTRIM(RTRIM(Name))) = 'SALES' THEN 'sale'
        WHEN UPPER(LTRIM(RTRIM(Name))) IN ('PURCHASE', 'PURCHASES') THEN 'purchase'
        WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(Name)), ' ', ''), '-', ''), '_', '')) IN ('USEDCARPURCHASE', 'USEDCARPURCHASES') THEN 'used_car_purchase'
        ELSE CONCAT('type_', Id)
    END
WHERE TypeKey IS NULL
   OR LTRIM(RTRIM(TypeKey)) = '';

UPDATE dbo.TransactionTypes
SET SortOrder = CASE
        WHEN TypeKey = 'sale' THEN 10
        WHEN TypeKey = 'purchase' THEN 20
        WHEN TypeKey = 'used_car_purchase' THEN 30
        ELSE 1000 + Id
    END
WHERE ISNULL(SortOrder, 0) = 0;

UPDATE dbo.TransactionTypes
SET SerialNumberFormat = CASE
        WHEN TypeKey = 'sale' THEN N'INV-{DATE:yyyyMMdd}-{NUMBER:00000000}'
        WHEN TypeKey = 'purchase' THEN N'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}'
        WHEN TypeKey = 'used_car_purchase' THEN N'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}'
        ELSE N'TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}'
    END
WHERE SerialNumberFormat IS NULL
   OR LTRIM(RTRIM(SerialNumberFormat)) = '';

UPDATE dbo.TransactionTypes
SET SerialStartNumber = 1
WHERE SerialStartNumber IS NULL
   OR SerialStartNumber <= 0;

UPDATE dbo.TransactionTypes
SET SerialCurrentNumber = 0
WHERE SerialCurrentNumber IS NULL
   OR SerialCurrentNumber < 0;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN TypeKey NVARCHAR(80) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.TransactionTypes')
      AND name = 'UX_TransactionTypes_TypeKey'
)
BEGIN
    CREATE UNIQUE INDEX UX_TransactionTypes_TypeKey
        ON dbo.TransactionTypes(TypeKey);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'sale')
BEGIN
    INSERT INTO dbo.TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES ('sale', 'Sales', 'USD', 1, 'INV-{DATE:yyyyMMdd}-{NUMBER:00000000}', 1, 0, 1, 10);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'purchase')
BEGIN
    INSERT INTO dbo.TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES ('purchase', 'Purchases', 'USD', 1, 'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}', 1, 0, 1, 20);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'used_car_purchase')
BEGIN
    INSERT INTO dbo.TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES ('used_car_purchase', 'Used Car Purchases', 'USD', 1, 'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}', 1, 0, 1, 30);
END;

IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
        TransactionTypeId INT NOT NULL,
        ReferenceId INT NOT NULL CONSTRAINT DF_Transactions_ReferenceId DEFAULT (0),
        TransactionNumber NVARCHAR(32) NOT NULL,
        TransactionDate DATETIME2(0) NOT NULL,
        CustomerId INT NULL,
        SupplierId INT NULL,
        WarehouseId INT NULL,
        UsedCarId INT NULL,
        Subtotal DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_Subtotal DEFAULT (0),
        DiscountAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_DiscountAmount DEFAULT (0),
        TaxAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_TaxAmount DEFAULT (0),
        TotalAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_TotalAmount DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_PaidAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Transactions_PaymentStatus DEFAULT (N'Unpaid'),
        PaymentMethod NVARCHAR(50) NULL,
        Notes NVARCHAR(1000) NULL,
        IsReturn BIT NOT NULL CONSTRAINT DF_Transactions_IsReturn DEFAULT (0),
        ParentReferenceId INT NULL,
        TotalCost DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_TotalCost DEFAULT (0),
        PostingStatus NVARCHAR(20) NULL,
        PostedAt DATETIME2(0) NULL,
        PostedByUserId INT NULL,
        BaseCurrencyCode CHAR(3) NULL,
        CounterCurrencyCode CHAR(3) NULL,
        TotalBaseAmount DECIMAL(19, 4) NULL,
        TotalCounterAmount DECIMAL(19, 4) NULL,
        PaidCounterAmount DECIMAL(19, 4) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Transactions_TransactionTypes FOREIGN KEY (TransactionTypeId) REFERENCES dbo.TransactionTypes(Id)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'UX_Transactions_Type_ReferenceId'
)
BEGIN
    CREATE UNIQUE INDEX UX_Transactions_Type_ReferenceId
        ON dbo.Transactions(TransactionTypeId, ReferenceId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'UX_Transactions_Type_Number'
)
BEGIN
    CREATE UNIQUE INDEX UX_Transactions_Type_Number
        ON dbo.Transactions(TransactionTypeId, TransactionNumber);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'IX_Transactions_Type_Date'
)
BEGIN
    CREATE INDEX IX_Transactions_Type_Date
        ON dbo.Transactions(TransactionTypeId, TransactionDate DESC, Id DESC);
END;

IF OBJECT_ID('dbo.TransactionItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TransactionItems PRIMARY KEY,
        TransactionId INT NOT NULL,
        ItemType NVARCHAR(40) NOT NULL,
        PartId INT NULL,
        AccountId INT NULL,
        DetailKey NVARCHAR(80) NULL, -- backfilled by ADD COLUMN guard below if table pre-existed
        Description NVARCHAR(160) NULL,
        Quantity DECIMAL(19, 4) NULL,
        UnitPrice DECIMAL(19, 4) NULL,
        UnitCost DECIMAL(19, 4) NULL,
        DiscountAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_TransactionItems_DiscountAmount DEFAULT (0),
        TaxRate DECIMAL(19, 4) NOT NULL CONSTRAINT DF_TransactionItems_TaxRate DEFAULT (0),
        Amount DECIMAL(19, 4) NULL,
        LineTotal DECIMAL(19, 4) NOT NULL CONSTRAINT DF_TransactionItems_LineTotal DEFAULT (0),
        CurrencyCode CHAR(3) NULL,
        RateToBase DECIMAL(19, 8) NULL,
        BaseAmount DECIMAL(19, 4) NULL,
        CounterAmount DECIMAL(19, 4) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_TransactionItems_SortOrder DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TransactionItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_TransactionItems_Transactions FOREIGN KEY (TransactionId) REFERENCES dbo.Transactions(Id) ON DELETE CASCADE
    );
END;

IF COL_LENGTH('dbo.TransactionItems', 'DetailKey') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionItems ADD DetailKey NVARCHAR(80) NULL;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.TransactionItems')
      AND name = 'IX_TransactionItems_TransactionId'
)
BEGIN
    CREATE INDEX IX_TransactionItems_TransactionId
        ON dbo.TransactionItems(TransactionId, SortOrder, Id);
END;

DECLARE @SaleTransactionTypeId INT;
DECLARE @PurchaseTransactionTypeId INT;
DECLARE @UsedCarPurchaseTransactionTypeId INT;

SELECT @SaleTransactionTypeId = Id FROM dbo.TransactionTypes WHERE TypeKey = 'sale';
SELECT @PurchaseTransactionTypeId = Id FROM dbo.TransactionTypes WHERE TypeKey = 'purchase';
SELECT @UsedCarPurchaseTransactionTypeId = Id FROM dbo.TransactionTypes WHERE TypeKey = 'used_car_purchase';

IF OBJECT_ID('dbo.SalesInvoices', 'U') IS NOT NULL AND @SaleTransactionTypeId IS NOT NULL
BEGIN
    INSERT INTO dbo.Transactions
    (
        TransactionTypeId,
        ReferenceId,
        TransactionNumber,
        TransactionDate,
        CustomerId,
        WarehouseId,
        Subtotal,
        DiscountAmount,
        TaxAmount,
        TotalAmount,
        PaidAmount,
        PaymentStatus,
        PaymentMethod,
        Notes,
        IsReturn,
        ParentReferenceId,
        TotalCost,
        BaseCurrencyCode,
        CounterCurrencyCode,
        TotalBaseAmount,
        TotalCounterAmount,
        PaidCounterAmount,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        @SaleTransactionTypeId,
        s.Id,
        s.InvoiceNumber,
        s.InvoiceDate,
        s.CustomerId,
        s.WarehouseId,
        s.Subtotal,
        s.DiscountAmount,
        s.TaxAmount,
        s.TotalAmount,
        s.PaidAmount,
        s.PaymentStatus,
        s.PaymentMethod,
        s.Notes,
        s.IsReturn,
        s.ParentInvoiceId,
        s.TotalCost,
        tt.CurrencyCode,
        tt.CurrencyCode,
        s.TotalAmount,
        s.TotalAmount,
        s.PaidAmount,
        s.CreatedAt,
        s.CreatedByUserId,
        s.ModifiedAt,
        s.ModifiedByUserId
    FROM dbo.SalesInvoices s
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = @SaleTransactionTypeId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions existing
        WHERE existing.TransactionTypeId = @SaleTransactionTypeId
          AND existing.ReferenceId = s.Id
    );

    INSERT INTO dbo.TransactionItems
    (
        TransactionId,
        ItemType,
        PartId,
        Quantity,
        UnitPrice,
        DiscountAmount,
        TaxRate,
        Amount,
        LineTotal,
        CurrencyCode,
        RateToBase,
        BaseAmount,
        CounterAmount,
        SortOrder,
        CreatedAt,
        CreatedByUserId
    )
    SELECT
        t.Id,
        N'sale_item',
        si.PartId,
        CAST(si.Quantity AS DECIMAL(19, 4)),
        si.UnitPrice,
        si.DiscountAmount,
        si.TaxRate,
        CAST(si.Quantity * si.UnitPrice AS DECIMAL(19, 4)),
        si.LineTotal,
        t.CounterCurrencyCode,
        1,
        si.LineTotal,
        si.LineTotal,
        ROW_NUMBER() OVER (PARTITION BY si.InvoiceId ORDER BY si.Id),
        si.CreatedAt,
        si.CreatedByUserId
    FROM dbo.SalesInvoiceItems si
    INNER JOIN dbo.Transactions t
        ON t.TransactionTypeId = @SaleTransactionTypeId
       AND t.ReferenceId = si.InvoiceId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TransactionItems existing
        WHERE existing.TransactionId = t.Id
    );
END;

IF OBJECT_ID('dbo.PurchaseInvoices', 'U') IS NOT NULL AND @PurchaseTransactionTypeId IS NOT NULL
BEGIN
    INSERT INTO dbo.Transactions
    (
        TransactionTypeId,
        ReferenceId,
        TransactionNumber,
        TransactionDate,
        SupplierId,
        WarehouseId,
        Subtotal,
        DiscountAmount,
        TaxAmount,
        TotalAmount,
        PaidAmount,
        PaymentStatus,
        BaseCurrencyCode,
        CounterCurrencyCode,
        TotalBaseAmount,
        TotalCounterAmount,
        PaidCounterAmount,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        @PurchaseTransactionTypeId,
        p.Id,
        p.PurchaseNumber,
        p.PurchaseDate,
        p.SupplierId,
        p.WarehouseId,
        p.Subtotal,
        p.DiscountAmount,
        p.TaxAmount,
        p.TotalAmount,
        p.PaidAmount,
        p.PaymentStatus,
        tt.CurrencyCode,
        tt.CurrencyCode,
        p.TotalAmount,
        p.TotalAmount,
        p.PaidAmount,
        p.CreatedAt,
        p.CreatedByUserId,
        p.ModifiedAt,
        p.ModifiedByUserId
    FROM dbo.PurchaseInvoices p
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = @PurchaseTransactionTypeId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions existing
        WHERE existing.TransactionTypeId = @PurchaseTransactionTypeId
          AND existing.ReferenceId = p.Id
    );

    INSERT INTO dbo.TransactionItems
    (
        TransactionId,
        ItemType,
        PartId,
        Quantity,
        UnitCost,
        TaxRate,
        Amount,
        LineTotal,
        CurrencyCode,
        RateToBase,
        BaseAmount,
        CounterAmount,
        SortOrder,
        CreatedAt,
        CreatedByUserId
    )
    SELECT
        t.Id,
        N'purchase_item',
        pi.PartId,
        CAST(pi.Quantity AS DECIMAL(19, 4)),
        pi.UnitCost,
        pi.TaxRate,
        CAST(pi.Quantity * pi.UnitCost AS DECIMAL(19, 4)),
        pi.LineTotal,
        t.CounterCurrencyCode,
        1,
        pi.LineTotal,
        pi.LineTotal,
        ROW_NUMBER() OVER (PARTITION BY pi.PurchaseId ORDER BY pi.Id),
        pi.CreatedAt,
        pi.CreatedByUserId
    FROM dbo.PurchaseInvoiceItems pi
    INNER JOIN dbo.Transactions t
        ON t.TransactionTypeId = @PurchaseTransactionTypeId
       AND t.ReferenceId = pi.PurchaseId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TransactionItems existing
        WHERE existing.TransactionId = t.Id
    );
END;

IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL AND @UsedCarPurchaseTransactionTypeId IS NOT NULL
BEGIN
    INSERT INTO dbo.Transactions
    (
        TransactionTypeId,
        ReferenceId,
        TransactionNumber,
        TransactionDate,
        SupplierId,
        UsedCarId,
        TotalAmount,
        PaidAmount,
        PaymentStatus,
        Notes,
        PostingStatus,
        PostedAt,
        PostedByUserId,
        BaseCurrencyCode,
        CounterCurrencyCode,
        TotalBaseAmount,
        TotalCounterAmount,
        PaidCounterAmount,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        @UsedCarPurchaseTransactionTypeId,
        p.Id,
        p.PurchaseNumber,
        p.PurchaseDate,
        p.SupplierId,
        p.UsedCarId,
        p.TotalBaseAmount,
        p.PaidAmount,
        p.PaymentStatus,
        p.Notes,
        p.PostingStatus,
        p.PostedAt,
        p.PostedByUserId,
        p.BaseCurrencyCode,
        p.CounterCurrencyCode,
        p.TotalBaseAmount,
        p.TotalCounterAmount,
        p.PaidCounterAmount,
        p.CreatedAt,
        p.CreatedByUserId,
        p.ModifiedAt,
        p.ModifiedByUserId
    FROM dbo.UsedCarPurchases p
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions existing
        WHERE existing.TransactionTypeId = @UsedCarPurchaseTransactionTypeId
          AND existing.ReferenceId = p.Id
    );

    INSERT INTO dbo.TransactionItems
    (
        TransactionId,
        ItemType,
        AccountId,
        DetailKey,
        Description,
        Amount,
        LineTotal,
        CurrencyCode,
        RateToBase,
        BaseAmount,
        CounterAmount,
        SortOrder,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        t.Id,
        N'used_car_purchase_line',
        l.AccountId,
        l.DetailKey,
        l.Description,
        l.Amount,
        l.BaseAmount,
        l.CurrencyCode,
        l.RateToBase,
        l.BaseAmount,
        l.CounterAmount,
        l.SortOrder,
        l.CreatedAt,
        l.CreatedByUserId,
        l.ModifiedAt,
        l.ModifiedByUserId
    FROM dbo.UsedCarPurchaseLines l
    INNER JOIN dbo.Transactions t
        ON t.TransactionTypeId = @UsedCarPurchaseTransactionTypeId
       AND t.ReferenceId = l.UsedCarPurchaseId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TransactionItems existing
        WHERE existing.TransactionId = t.Id
    );
END;

UPDATE dbo.Transactions
SET ReferenceId = Id
WHERE ReferenceId <= 0;

;WITH ParsedTransactionNumbers AS
(
    SELECT t.TransactionTypeId,
           CASE
               WHEN trailing.DigitCount > 0
                   THEN TRY_CONVERT(BIGINT, RIGHT(LTRIM(RTRIM(t.TransactionNumber)), trailing.DigitCount))
               ELSE NULL
           END AS ParsedNumber
    FROM dbo.Transactions t
    OUTER APPLY
    (
        SELECT CASE
            WHEN t.TransactionNumber IS NULL THEN 0
            ELSE PATINDEX('%[^0-9]%', REVERSE(LTRIM(RTRIM(t.TransactionNumber))) + 'X') - 1
        END AS DigitCount
    ) trailing
),
TransactionTypeMaxNumbers AS
(
    SELECT TransactionTypeId,
           MAX(ParsedNumber) AS MaxIssuedNumber
    FROM ParsedTransactionNumbers
    GROUP BY TransactionTypeId
)
UPDATE tt
SET SerialCurrentNumber = CASE
        WHEN ISNULL(m.MaxIssuedNumber, 0) > ISNULL(tt.SerialCurrentNumber, 0) THEN ISNULL(m.MaxIssuedNumber, 0)
        ELSE ISNULL(tt.SerialCurrentNumber, 0)
    END
FROM dbo.TransactionTypes tt
LEFT JOIN TransactionTypeMaxNumbers m ON m.TransactionTypeId = tt.Id;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN SerialNumberFormat NVARCHAR(200) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN SerialStartNumber BIGINT NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN SerialCurrentNumber BIGINT NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

UPDATE je
SET Description = LEFT(
        CONCAT(
            N'Used car receipt - ',
            CASE
                WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                        ELSE cm.Name + N' (' + cm.BodyType + N')'
                    END
                ELSE
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                        ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                    END
            END,
            N' ',
            CONVERT(NVARCHAR(4), uc.ModelYear)
        ),
        400)
FROM dbo.JournalEntries je
INNER JOIN dbo.UsedCars uc ON uc.Id = je.ReferenceId
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE je.ReferenceType = N'UsedCar';

UPDATE je
SET Description = LEFT(
        CONCAT(
            N'Used car purchase - ',
            CASE
                WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                        ELSE cm.Name + N' (' + cm.BodyType + N')'
                    END
                ELSE
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                        ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                    END
            END,
            N' ',
            CONVERT(NVARCHAR(4), uc.ModelYear),
            CASE
                WHEN NULLIF(LTRIM(RTRIM(t.TransactionNumber)), N'') IS NULL THEN N''
                ELSE N' (' + LTRIM(RTRIM(t.TransactionNumber)) + N')'
            END
        ),
        400)
FROM dbo.JournalEntries je
INNER JOIN dbo.Transactions t ON t.ReferenceId = je.ReferenceId
INNER JOIN dbo.TransactionTypes tt
    ON tt.Id = t.TransactionTypeId
   AND tt.TypeKey = 'used_car_purchase'
INNER JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE je.ReferenceType = N'UsedCarPurchase';

UPDATE je
SET Description = LEFT(
        CONCAT(
            N'Used car purchase payment - ',
            CASE
                WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                        ELSE cm.Name + N' (' + cm.BodyType + N')'
                    END
                ELSE
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                        ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                    END
            END,
            N' ',
            CONVERT(NVARCHAR(4), uc.ModelYear),
            CASE
                WHEN NULLIF(LTRIM(RTRIM(t.TransactionNumber)), N'') IS NULL THEN N''
                ELSE N' (' + LTRIM(RTRIM(t.TransactionNumber)) + N')'
            END
        ),
        400)
FROM dbo.JournalEntries je
INNER JOIN dbo.Transactions t ON t.ReferenceId = je.ReferenceId
INNER JOIN dbo.TransactionTypes tt
    ON tt.Id = t.TransactionTypeId
   AND tt.TypeKey = 'used_car_purchase'
INNER JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE je.ReferenceType = N'UsedCarPurchasePayment';
GO

-- ── BarcodeScanningMigration ──────────────────────────────────────────────
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Parts', 'Barcode') IS NULL
            BEGIN
                ALTER TABLE dbo.Parts ADD Barcode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Warehouses', 'Barcode') IS NULL
            BEGIN
                ALTER TABLE dbo.Warehouses ADD Barcode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.UsedCars', 'Barcode') IS NULL
            BEGIN
                ALTER TABLE dbo.UsedCars ADD Barcode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.StockMovements', 'ScanCode') IS NULL
            BEGIN
                ALTER TABLE dbo.StockMovements ADD ScanCode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Transactions', 'ScanCode') IS NULL
            BEGIN
                ALTER TABLE dbo.Transactions ADD ScanCode NVARCHAR(120) NULL;
            END;

            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Parts', 'Barcode') IS NOT NULL
            BEGIN
                UPDATE dbo.Parts
                SET Barcode = NULLIF(LTRIM(RTRIM(InternalCode)), N'')
                WHERE (Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'')
                  AND NULLIF(LTRIM(RTRIM(InternalCode)), N'') IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Warehouses', 'Barcode') IS NOT NULL
            BEGIN
                UPDATE dbo.Warehouses
                SET Barcode = CONCAT(N'WH-', Id)
                WHERE Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'';
            END;

            IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.UsedCars', 'Barcode') IS NOT NULL
            BEGIN
                UPDATE dbo.UsedCars
                SET Barcode = CONCAT(N'UC-', Id)
                WHERE Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'';
            END;

            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.StockMovements', 'ScanCode') IS NOT NULL
            BEGIN
                UPDATE dbo.StockMovements
                SET ScanCode = CONCAT(N'SM-', Id)
                WHERE ScanCode IS NULL OR LTRIM(RTRIM(ScanCode)) = N'';
            END;

            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
               AND COL_LENGTH('dbo.Transactions', 'ScanCode') IS NOT NULL
            BEGIN
                UPDATE dbo.Transactions
                SET ScanCode = NULLIF(LTRIM(RTRIM(TransactionNumber)), N'')
                WHERE (ScanCode IS NULL OR LTRIM(RTRIM(ScanCode)) = N'')
                  AND NULLIF(LTRIM(RTRIM(TransactionNumber)), N'') IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Parts') AND name = 'IX_Parts_Barcode')
            BEGIN
                CREATE INDEX IX_Parts_Barcode ON dbo.Parts(Barcode) WHERE Barcode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Warehouses') AND name = 'IX_Warehouses_Barcode')
            BEGIN
                CREATE INDEX IX_Warehouses_Barcode ON dbo.Warehouses(Barcode) WHERE Barcode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.UsedCars') AND name = 'IX_UsedCars_Barcode')
            BEGIN
                CREATE INDEX IX_UsedCars_Barcode ON dbo.UsedCars(Barcode) WHERE Barcode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.StockMovements') AND name = 'IX_StockMovements_ScanCode')
            BEGIN
                CREATE INDEX IX_StockMovements_ScanCode ON dbo.StockMovements(ScanCode) WHERE ScanCode IS NOT NULL;
            END;

            IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
               AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Transactions') AND name = 'IX_Transactions_ScanCode')
            BEGIN
                CREATE INDEX IX_Transactions_ScanCode ON dbo.Transactions(ScanCode) WHERE ScanCode IS NOT NULL;
            END;
GO

-- ── PartRequestsMigration ─────────────────────────────────────────────────
IF OBJECT_ID('dbo.PartRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartRequests
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartRequests PRIMARY KEY,
        PartId INT NULL,
        CustomerId INT NULL,
        CustomerName NVARCHAR(200) NOT NULL,
        CustomerPhone NVARCHAR(80) NULL,
        RequestedPartName NVARCHAR(240) NOT NULL,
        RequestedOemNumber NVARCHAR(120) NULL,
        VehicleDetails NVARCHAR(240) NULL,
        Quantity INT NOT NULL CONSTRAINT DF_PartRequests_Quantity DEFAULT 1,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_PartRequests_Status DEFAULT N'Open',
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        ClosedAt DATETIME2(0) NULL,
        ClosedByUserId INT NULL,
        CONSTRAINT FK_PartRequests_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_PartRequests_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_PartRequests_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_PartRequests_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_PartRequests_ClosedByUsers FOREIGN KEY (ClosedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_PartRequests_Quantity_Positive CHECK (Quantity > 0),
        CONSTRAINT CK_PartRequests_Status CHECK (Status IN (N'Open', N'Contacted', N'Fulfilled', N'Cancelled'))
    );
END;

IF OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PartRequests') AND name = 'IX_PartRequests_Status_CreatedAt')
BEGIN
    CREATE INDEX IX_PartRequests_Status_CreatedAt ON dbo.PartRequests (Status, CreatedAt DESC);
END;

IF OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PartRequests') AND name = 'IX_PartRequests_PartId')
BEGIN
    CREATE INDEX IX_PartRequests_PartId ON dbo.PartRequests (PartId) WHERE PartId IS NOT NULL;
END;

IF OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID('dbo.PartRequests')
          AND name = N'CK_PartRequests_Status')
    BEGIN
        ALTER TABLE dbo.PartRequests DROP CONSTRAINT CK_PartRequests_Status;
    END;

    ALTER TABLE dbo.PartRequests
    ADD CONSTRAINT CK_PartRequests_Status CHECK (Status IN (N'Open', N'Contacted', N'Reserved', N'Fulfilled', N'Cancelled'));
END;

IF OBJECT_ID('dbo.PartRequestReservations', 'U') IS NULL
   AND OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Stock', 'U') IS NOT NULL
BEGIN
    CREATE TABLE dbo.PartRequestReservations
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartRequestReservations PRIMARY KEY,
        PartRequestId INT NOT NULL,
        StockId INT NOT NULL,
        Quantity INT NOT NULL,
        ExpiresAt DATETIME2(0) NOT NULL,
        ExpirationAction NVARCHAR(30) NOT NULL CONSTRAINT DF_PartRequestReservations_ExpirationAction DEFAULT N'AutoRelease',
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_PartRequestReservations_Status DEFAULT N'Active',
        ReminderSentAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartRequestReservations_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ReleasedAt DATETIME2(0) NULL,
        ReleasedByUserId INT NULL,
        ReleaseReason NVARCHAR(120) NULL,
        CONSTRAINT FK_PartRequestReservations_PartRequests FOREIGN KEY (PartRequestId) REFERENCES dbo.PartRequests (Id) ON DELETE CASCADE,
        CONSTRAINT FK_PartRequestReservations_Stock FOREIGN KEY (StockId) REFERENCES dbo.Stock (Id),
        CONSTRAINT FK_PartRequestReservations_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_PartRequestReservations_ReleasedByUsers FOREIGN KEY (ReleasedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT CK_PartRequestReservations_Quantity_Positive CHECK (Quantity > 0),
        CONSTRAINT CK_PartRequestReservations_ExpirationAction CHECK (ExpirationAction IN (N'AutoRelease', N'StaffReminder')),
        CONSTRAINT CK_PartRequestReservations_Status CHECK (Status IN (N'Active', N'Released', N'Fulfilled'))
    );
END;

IF OBJECT_ID('dbo.PartRequestReservations', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.PartRequestReservations', 'ReminderSentAt') IS NULL
BEGIN
    ALTER TABLE dbo.PartRequestReservations ADD ReminderSentAt DATETIME2(0) NULL;
END;

IF OBJECT_ID('dbo.PartRequestReservations', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PartRequestReservations') AND name = 'IX_PartRequestReservations_ActiveDeadline')
BEGIN
    CREATE INDEX IX_PartRequestReservations_ActiveDeadline
    ON dbo.PartRequestReservations (Status, ExpiresAt, ExpirationAction)
    INCLUDE (PartRequestId, Quantity, ReminderSentAt);
END;

IF OBJECT_ID('dbo.PartRequestReservations', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.PartRequestReservations') AND name = 'IX_PartRequestReservations_PartRequestId')
BEGIN
    CREATE INDEX IX_PartRequestReservations_PartRequestId
    ON dbo.PartRequestReservations (PartRequestId, Status);
END;
GO

-- ── PartUsedCarStockMigration ─────────────────────────────────────────────
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Stock', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    DECLARE @UsedCarPartWarehouseId INT;
    DECLARE @UsedCarPartStockLocationId INT;

    SELECT TOP (1) @UsedCarPartWarehouseId = Id
    FROM dbo.Warehouses
    ORDER BY CASE WHEN IsMain = 1 THEN 0 ELSE 1 END, Id;

    IF @UsedCarPartWarehouseId IS NOT NULL
    BEGIN
        IF OBJECT_ID('dbo.Locations', 'U') IS NOT NULL
        BEGIN
            SELECT TOP (1) @UsedCarPartStockLocationId = Id
            FROM dbo.Locations
            WHERE WarehouseId = @UsedCarPartWarehouseId
            ORDER BY Id;
        END;

        DECLARE @UsedCarPartStockSeed TABLE
        (
            PartId INT NOT NULL PRIMARY KEY,
            UsedCarId INT NOT NULL,
            QuantityToAdd INT NOT NULL,
            UnitCost DECIMAL(18, 2) NOT NULL
        );

        INSERT INTO @UsedCarPartStockSeed
            (PartId, UsedCarId, QuantityToAdd, UnitCost)
        SELECT p.Id,
               p.UsedCarId,
               1 - stock.CurrentQuantity,
               ISNULL(p.CostPrice, 0)
        FROM dbo.Parts p
        INNER JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
        OUTER APPLY
        (
            SELECT CurrentQuantity = ISNULL(SUM(s.Quantity), 0)
            FROM dbo.Stock s
            WHERE s.PartId = p.Id
        ) stock
        WHERE p.UsedCarId IS NOT NULL
          AND stock.CurrentQuantity < 1
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.StockMovements sm
              WHERE sm.PartId = p.Id
                AND sm.Quantity < 0
                AND (sm.MovementType IN (N'Sale', N'2') OR TRY_CONVERT(INT, sm.MovementType) = 2)
          );

        UPDATE existingStock
        SET Quantity = existingStock.Quantity + seed.QuantityToAdd,
            ModifiedAt = SYSUTCDATETIME()
        FROM dbo.Stock existingStock
        INNER JOIN @UsedCarPartStockSeed seed
            ON seed.PartId = existingStock.PartId
        WHERE existingStock.WarehouseId = @UsedCarPartWarehouseId;

        INSERT INTO dbo.Stock
            (PartId, WarehouseId, LocationId, Quantity, ReservedQuantity, CreatedAt, CreatedByUserId)
        SELECT seed.PartId,
               @UsedCarPartWarehouseId,
               @UsedCarPartStockLocationId,
               seed.QuantityToAdd,
               0,
               SYSUTCDATETIME(),
               NULL
        FROM @UsedCarPartStockSeed seed
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.Stock existingStock
            WHERE existingStock.PartId = seed.PartId
              AND existingStock.WarehouseId = @UsedCarPartWarehouseId
        );

        INSERT INTO dbo.StockMovements
            (PartId, WarehouseId, Quantity, MovementType, ReferenceType, ReferenceId, UnitCost, CreatedAt, CreatedByUserId)
        SELECT seed.PartId,
               @UsedCarPartWarehouseId,
               seed.QuantityToAdd,
               N'Adjust',
               N'UsedCar',
               seed.UsedCarId,
               seed.UnitCost,
               SYSUTCDATETIME(),
               NULL
        FROM @UsedCarPartStockSeed seed;
    END;
END;
GO

-- ── UsedCarImagesMigration ────────────────────────────────────────────────
IF OBJECT_ID('dbo.usedcar_images', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.usedcar_images
    (
        ImageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_usedcar_images PRIMARY KEY,
        UsedCarId INT NOT NULL,
        ImageData VARBINARY(MAX) NOT NULL,
        ImageMimeType NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_usedcar_images_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_usedcar_images_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars (Id) ON DELETE CASCADE,
        CONSTRAINT FK_usedcar_images_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_usedcar_images_UsedCarId ON dbo.usedcar_images (UsedCarId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_usedcar_images_UsedCarId'
      AND object_id = OBJECT_ID('dbo.usedcar_images'))
BEGIN
    CREATE INDEX IX_usedcar_images_UsedCarId ON dbo.usedcar_images (UsedCarId);
END;
GO

-- ── CommunicationsMigration ───────────────────────────────────────────────
IF OBJECT_ID('dbo.OutboundMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundMessages
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OutboundMessages PRIMARY KEY,
        Direction NVARCHAR(20) NOT NULL CONSTRAINT DF_OutboundMessages_Direction DEFAULT (N'Outbound'),
        Channel NVARCHAR(20) NOT NULL,
        RecipientKind NVARCHAR(20) NOT NULL,
        RecipientId INT NULL,
        RecipientName NVARCHAR(200) NOT NULL,
        RecipientPhone NVARCHAR(50) NOT NULL,
        TemplateKey NVARCHAR(60) NOT NULL,
        ReferenceType NVARCHAR(60) NOT NULL,
        ReferenceId INT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        AttachmentCount INT NOT NULL CONSTRAINT DF_OutboundMessages_AttachmentCount DEFAULT (0),
        Status NVARCHAR(30) NOT NULL,
        Provider NVARCHAR(80) NULL,
        ProviderMessageId NVARCHAR(200) NULL,
        ProviderStatus NVARCHAR(200) NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_OutboundMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        SentAt DATETIME2(0) NULL,
        CONSTRAINT FK_OutboundMessages_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );
END;

IF COL_LENGTH('dbo.OutboundMessages', 'Direction') IS NULL
BEGIN
    ALTER TABLE dbo.OutboundMessages
    ADD Direction NVARCHAR(20) NOT NULL CONSTRAINT DF_OutboundMessages_Direction DEFAULT (N'Outbound') WITH VALUES;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboundMessages_CreatedAt'
      AND object_id = OBJECT_ID('dbo.OutboundMessages'))
BEGIN
    CREATE INDEX IX_OutboundMessages_CreatedAt
        ON dbo.OutboundMessages (CreatedAt DESC, Id DESC);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboundMessages_RecipientPhone'
      AND object_id = OBJECT_ID('dbo.OutboundMessages'))
BEGIN
    CREATE INDEX IX_OutboundMessages_RecipientPhone
        ON dbo.OutboundMessages (RecipientPhone, CreatedAt DESC, Id DESC);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboundMessages_Reference'
      AND object_id = OBJECT_ID('dbo.OutboundMessages'))
BEGIN
    CREATE INDEX IX_OutboundMessages_Reference
        ON dbo.OutboundMessages (ReferenceType, ReferenceId, CreatedAt DESC);
END;
GO

-- ── WhatsAppCampaignsMigration ────────────────────────────────────────────
IF OBJECT_ID('dbo.WhatsAppCampaigns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WhatsAppCampaigns
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppCampaigns PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        Segment NVARCHAR(60) NOT NULL,
        Language NVARCHAR(30) NOT NULL,
        MessageBody NVARCHAR(MAX) NOT NULL,
        SelectedPartIds NVARCHAR(MAX) NULL,
        SelectedUsedCarIds NVARCHAR(MAX) NULL,
        RecipientCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_RecipientCount DEFAULT (0),
        SentCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_SentCount DEFAULT (0),
        FailedCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_FailedCount DEFAULT (0),
        PreparedCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_PreparedCount DEFAULT (0),
        AttachmentCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_AttachmentCount DEFAULT (0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WhatsAppCampaigns_Status DEFAULT (N'Draft'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WhatsAppCampaigns_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_WhatsAppCampaigns_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );
END;

IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WhatsAppCampaignRecipients
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppCampaignRecipients PRIMARY KEY,
        CampaignId INT NOT NULL,
        CustomerId INT NULL,
        RecipientName NVARCHAR(200) NOT NULL,
        RecipientPhone NVARCHAR(50) NOT NULL,
        MessageId INT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WhatsAppCampaignRecipients_Status DEFAULT (N'Prepared'),
        ErrorMessage NVARCHAR(1000) NULL,
        SentAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WhatsAppCampaignRecipients_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_WhatsAppCampaignRecipients_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.WhatsAppCampaigns (Id) ON DELETE CASCADE,
        CONSTRAINT FK_WhatsAppCampaignRecipients_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_WhatsAppCampaignRecipients_Messages FOREIGN KEY (MessageId) REFERENCES dbo.OutboundMessages (Id)
    );
END;

IF OBJECT_ID('dbo.WhatsAppCampaigns', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.WhatsAppCampaigns') AND name = 'IX_WhatsAppCampaigns_CreatedAt')
BEGIN
    CREATE INDEX IX_WhatsAppCampaigns_CreatedAt ON dbo.WhatsAppCampaigns (CreatedAt DESC, Id DESC);
END;

IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.WhatsAppCampaignRecipients') AND name = 'IX_WhatsAppCampaignRecipients_CampaignId')
BEGIN
    CREATE INDEX IX_WhatsAppCampaignRecipients_CampaignId ON dbo.WhatsAppCampaignRecipients (CampaignId, Id);
END;

IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.WhatsAppCampaignRecipients') AND name = 'IX_WhatsAppCampaignRecipients_Phone')
BEGIN
    CREATE INDEX IX_WhatsAppCampaignRecipients_Phone ON dbo.WhatsAppCampaignRecipients (RecipientPhone, CampaignId);
END;
GO

-- ── ReportBuilderLinksMigration ───────────────────────────────────────────
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
   OR JoinType <> UPPER(LTRIM(RTRIM(ISNULL(JoinType, 'LEFT'))));
GO

-- ── ReportBuilderAdvancedMigration ────────────────────────────────────────
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
        RoleId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CanView DEFAULT (1),
        CanEdit BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CanEdit DEFAULT (0),
        CanExport BIT NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CanExport DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReportBuilderSavedReportRoles_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_ReportBuilderSavedReportRoles PRIMARY KEY (ReportId, RoleId),
        CONSTRAINT FK_ReportBuilderSavedReportRoles_Report FOREIGN KEY (ReportId) REFERENCES dbo.ReportBuilderSavedReports(Id) ON DELETE CASCADE,
        CONSTRAINT FK_ReportBuilderSavedReportRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
    );
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReportRoles', 'RoleId') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReportRoles ADD RoleId INT NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReportRoles', 'RoleName') IS NOT NULL
BEGIN
    EXEC(N'
UPDATE rr
SET RoleId = r.Id
FROM dbo.ReportBuilderSavedReportRoles rr
INNER JOIN dbo.Roles r ON r.Name = rr.RoleName
WHERE rr.RoleId IS NULL;');
END;

EXEC(N'
DELETE rr
FROM dbo.ReportBuilderSavedReportRoles rr
WHERE rr.RoleId IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = rr.RoleId);');

IF EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ReportBuilderSavedReportRoles')
      AND [type] = 'PK'
      AND name = N'PK_ReportBuilderSavedReportRoles'
      AND EXISTS (
          SELECT 1
          FROM sys.index_columns ic
          INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
          WHERE ic.object_id = OBJECT_ID(N'dbo.ReportBuilderSavedReportRoles')
            AND ic.index_id = (SELECT unique_index_id FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ReportBuilderSavedReportRoles') AND name = N'PK_ReportBuilderSavedReportRoles')
            AND c.name = N'RoleName'
      )
)
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReportRoles DROP CONSTRAINT PK_ReportBuilderSavedReportRoles;
END;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ReportBuilderSavedReportRoles')
      AND name = N'RoleId'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReportRoles ALTER COLUMN RoleId INT NOT NULL;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.key_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ReportBuilderSavedReportRoles')
      AND [type] = 'PK'
      AND name = N'PK_ReportBuilderSavedReportRoles'
)
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReportRoles
    ADD CONSTRAINT PK_ReportBuilderSavedReportRoles PRIMARY KEY (ReportId, RoleId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.ReportBuilderSavedReportRoles')
      AND name = N'FK_ReportBuilderSavedReportRoles_Roles'
)
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReportRoles WITH CHECK
    ADD CONSTRAINT FK_ReportBuilderSavedReportRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id);
END;

IF COL_LENGTH('dbo.ReportBuilderSavedReportRoles', 'RoleName') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderSavedReportRoles DROP COLUMN RoleName;
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
        RequestedByRoleId INT NULL,
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

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'RequestedByRoleId') IS NULL
BEGIN
    ALTER TABLE dbo.ReportBuilderBackgroundRuns ADD RequestedByRoleId INT NULL;
END;

IF COL_LENGTH('dbo.ReportBuilderBackgroundRuns', 'RequestedByRoleName') IS NOT NULL
BEGIN
    EXEC(N'
UPDATE br
SET RequestedByRoleId = r.Id
FROM dbo.ReportBuilderBackgroundRuns br
INNER JOIN dbo.Roles r ON r.Name = br.RequestedByRoleName
WHERE br.RequestedByRoleId IS NULL;');

    ALTER TABLE dbo.ReportBuilderBackgroundRuns DROP COLUMN RequestedByRoleName;
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
END;
GO

-- ── ReorderRulesMigration ─────────────────────────────────────────────────
IF OBJECT_ID('dbo.ReorderRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReorderRules
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReorderRules PRIMARY KEY,
        PartId INT NOT NULL,
        ReorderPoint INT NOT NULL CONSTRAINT DF_ReorderRules_ReorderPoint DEFAULT 0,
        ReorderQuantity INT NOT NULL CONSTRAINT DF_ReorderRules_ReorderQuantity DEFAULT 1,
        PreferredSupplierId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReorderRules_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReorderRules_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_ReorderRules_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT UQ_ReorderRules_PartId UNIQUE (PartId)
    );
END;
GO

-- ── PartSubstitutesMigration ──────────────────────────────────────────────
IF OBJECT_ID('dbo.PartSubstitutes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartSubstitutes
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartSubstitutes PRIMARY KEY,
        PartId INT NOT NULL,
        SubstitutePartId INT NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartSubstitutes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_PartSubstitutes_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_PartSubstitutes_SubstituteParts FOREIGN KEY (SubstitutePartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT UQ_PartSubstitutes_Pair UNIQUE (PartId, SubstitutePartId),
        CONSTRAINT CK_PartSubstitutes_NoSelfRef CHECK (PartId <> SubstitutePartId)
    );
END;
GO

-- ── PartExpiryMigration ───────────────────────────────────────────────────
IF COL_LENGTH('dbo.Parts', 'ExpiryDate') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD ExpiryDate DATETIME2(0) NULL;
END;

IF COL_LENGTH('dbo.Parts', 'ShelfLifeDays') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD ShelfLifeDays INT NULL;
END;
GO

-- ── CustomerLoyaltyMigration ──────────────────────────────────────────────
IF OBJECT_ID('dbo.LoyaltyTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoyaltyTransactions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LoyaltyTransactions PRIMARY KEY,
        CustomerId INT NOT NULL,
        Points INT NOT NULL,
        TransactionType NVARCHAR(50) NOT NULL CONSTRAINT DF_LoyaltyTransactions_Type DEFAULT N'Adjustment',
        ReferenceType NVARCHAR(50) NULL,
        ReferenceId INT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_LoyaltyTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_LoyaltyTransactions_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;

IF COL_LENGTH('dbo.Customers', 'LoyaltyPoints') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD LoyaltyPoints INT NOT NULL CONSTRAINT DF_Customers_LoyaltyPoints DEFAULT 0;
END;
GO

-- ── CustomerPriceTierMigration ────────────────────────────────────────────
IF COL_LENGTH('dbo.Customers', 'PriceTier') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD PriceTier INT NOT NULL CONSTRAINT DF_Customers_PriceTier DEFAULT 0;
END;
GO

-- ── WarrantyClaimsMigration ───────────────────────────────────────────────
IF OBJECT_ID('dbo.WarrantyClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaims
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarrantyClaims PRIMARY KEY,
        ClaimNumber NVARCHAR(50) NOT NULL CONSTRAINT DF_WarrantyClaims_ClaimNumber DEFAULT N'',
        CustomerId INT NULL,
        PartId INT NOT NULL,
        Quantity INT NOT NULL CONSTRAINT DF_WarrantyClaims_Quantity DEFAULT 1,
        ClaimType NVARCHAR(50) NOT NULL CONSTRAINT DF_WarrantyClaims_ClaimType DEFAULT N'Return',
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_WarrantyClaims_Status DEFAULT N'Open',
        Description NVARCHAR(1000) NULL,
        Resolution NVARCHAR(1000) NULL,
        OriginalInvoiceId INT NULL,
        RefundAmount DECIMAL(19,4) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WarrantyClaims_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ResolvedAt DATETIME2(0) NULL,
        ResolvedByUserId INT NULL,
        CONSTRAINT FK_WarrantyClaims_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_WarrantyClaims_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;
GO

-- ── SupplierPriceHistoryMigration ─────────────────────────────────────────
IF OBJECT_ID('dbo.SupplierPriceHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupplierPriceHistory
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupplierPriceHistory PRIMARY KEY,
        PartId INT NOT NULL,
        SupplierId INT NOT NULL,
        UnitPrice DECIMAL(19,4) NOT NULL,
        CurrencyCode NVARCHAR(3) NULL CONSTRAINT DF_SupplierPriceHistory_CurrencyCode DEFAULT N'USD',
        Quantity INT NOT NULL CONSTRAINT DF_SupplierPriceHistory_Quantity DEFAULT 1,
        InvoiceId INT NULL,
        RecordedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SupplierPriceHistory_RecordedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_SupplierPriceHistory_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_SupplierPriceHistory_Suppliers FOREIGN KEY (SupplierId) REFERENCES dbo.Suppliers (Id)
    );
END;

IF OBJECT_ID('dbo.SupplierPriceHistory', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.SupplierPriceHistory') AND name = 'IX_SupplierPriceHistory_PartId')
BEGIN
    CREATE INDEX IX_SupplierPriceHistory_PartId ON dbo.SupplierPriceHistory (PartId, SupplierId, RecordedAt DESC);
END;
GO

-- ── ShipmentsMigration ────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Shipments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Shipments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Shipments PRIMARY KEY,
        ShipmentNumber NVARCHAR(50) NOT NULL CONSTRAINT DF_Shipments_ShipmentNumber DEFAULT N'',
        SalesInvoiceId INT NULL,
        CustomerId INT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Shipments_Status DEFAULT N'Pending',
        CarrierName NVARCHAR(200) NULL,
        TrackingNumber NVARCHAR(100) NULL,
        DeliveryAddress NVARCHAR(500) NULL,
        EstimatedDelivery DATETIME2(0) NULL,
        ActualDelivery DATETIME2(0) NULL,
        Notes NVARCHAR(1000) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Shipments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Shipments_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;

IF OBJECT_ID('dbo.ShipmentEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ShipmentEvents
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ShipmentEvents PRIMARY KEY,
        ShipmentId INT NOT NULL,
        Status NVARCHAR(50) NOT NULL,
        Location NVARCHAR(200) NULL,
        Notes NVARCHAR(1000) NULL,
        EventAt DATETIME2(0) NOT NULL CONSTRAINT DF_ShipmentEvents_EventAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_ShipmentEvents_Shipments FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments (Id)
    );
END;
GO

-- ── ActivityLogMigration ──────────────────────────────────────────────────
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
GO

-- ── AccountingCurrencyRateRepairMigration ──────────────────────────────────
DECLARE @CRR_Base    CHAR(3)       = 'USD';
DECLARE @CRR_Counter CHAR(3)       = 'USD';
DECLARE @CRR_Rate    DECIMAL(19,8) = 1;

IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    SELECT TOP (1) @CRR_Base = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants
    WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode')
    ORDER BY CASE WHEN [Key] = 'BaseCurrencyCode' THEN 0 ELSE 1 END;

    SELECT TOP (1) @CRR_Counter = UPPER(LTRIM(RTRIM([Value])))
    FROM dbo.AppConstants WHERE [Key] = 'CounterCurrencyCode';

    SELECT TOP (1) @CRR_Rate = TRY_CONVERT(DECIMAL(19,8), [Value])
    FROM dbo.AppConstants WHERE [Key] = 'DefaultCounterRate';

    IF OBJECT_ID('dbo.CurrencyRates', 'U') IS NOT NULL
    BEGIN
        DECLARE @CRR_TableRate DECIMAL(19,8);
        SELECT TOP (1) @CRR_TableRate = TRY_CONVERT(DECIMAL(19,8), RateToUsd)
        FROM dbo.CurrencyRates
        WHERE UPPER(LTRIM(RTRIM(Code))) = @CRR_Counter AND RateToUsd > 0;
        IF @CRR_TableRate IS NOT NULL AND @CRR_TableRate > 0
            SET @CRR_Rate = @CRR_TableRate;
    END;
END;

SET @CRR_Base    = COALESCE(NULLIF(@CRR_Base, ''),    'USD');
SET @CRR_Counter = COALESCE(NULLIF(@CRR_Counter, ''), @CRR_Base, 'USD');
SET @CRR_Rate    = COALESCE(NULLIF(@CRR_Rate, 0),     1);

IF @CRR_Rate > 0 AND @CRR_Base <> @CRR_Counter
BEGIN
    IF OBJECT_ID('dbo.JournalLines', 'U') IS NOT NULL
       AND OBJECT_ID('dbo.JournalEntries', 'U') IS NOT NULL
    BEGIN
        ;WITH CandidateLines AS
        (
            SELECT jl.Id,
                   Amount = ROUND(CASE
                       WHEN ISNULL(jl.CounterAmount, 0)  > 0 THEN jl.CounterAmount
                       WHEN ISNULL(jl.OriginalAmount, 0) > 0 THEN jl.OriginalAmount
                       WHEN ISNULL(jl.Debit, 0)          > 0 THEN jl.Debit
                       ELSE jl.Credit END, 4)
            FROM dbo.JournalLines jl
            INNER JOIN dbo.JournalEntries je ON je.Id = jl.JournalEntryId
            WHERE (
                      (je.ReferenceType IN (N'Sale', N'SalePayment')
                       AND UPPER(LTRIM(RTRIM(ISNULL(jl.BaseCurrencyCode,    N'')))) = @CRR_Base
                       AND UPPER(LTRIM(RTRIM(ISNULL(jl.CounterCurrencyCode, N'')))) = @CRR_Counter)
                   OR (je.ReferenceType = N'Manual'
                       AND (UPPER(LTRIM(RTRIM(ISNULL(jl.BaseCurrencyCode,    N'')))) <> @CRR_Base
                         OR UPPER(LTRIM(RTRIM(ISNULL(jl.CounterCurrencyCode, N'')))) <> @CRR_Counter
                         OR UPPER(LTRIM(RTRIM(ISNULL(jl.BaseCurrencyCode,    N'')))) =  UPPER(LTRIM(RTRIM(ISNULL(jl.CounterCurrencyCode, N''))))))
                  )
              AND UPPER(LTRIM(RTRIM(ISNULL(jl.CurrencyCode, N'')))) = @CRR_Counter
              AND ROUND(ISNULL(jl.RateToBase, 0), 8) = 1
              AND ((ISNULL(jl.Debit, 0)  > 0 AND ABS(ISNULL(jl.Debit, 0)  - COALESCE(NULLIF(jl.CounterAmount, 0), NULLIF(jl.OriginalAmount, 0), jl.Debit))  <= 0.01)
                OR (ISNULL(jl.Credit, 0) > 0 AND ABS(ISNULL(jl.Credit, 0) - COALESCE(NULLIF(jl.CounterAmount, 0), NULLIF(jl.OriginalAmount, 0), jl.Credit)) <= 0.01))
        )
        UPDATE jl
           SET Debit               = CASE WHEN jl.Debit  > 0 THEN ROUND(c.Amount * @CRR_Rate, 4) ELSE 0 END,
               Credit              = CASE WHEN jl.Credit > 0 THEN ROUND(c.Amount * @CRR_Rate, 4) ELSE 0 END,
               CurrencyCode        = @CRR_Counter,
               OriginalAmount      = c.Amount,
               RateToBase          = @CRR_Rate,
               CounterAmount       = c.Amount,
               BaseCurrencyCode    = @CRR_Base,
               CounterCurrencyCode = @CRR_Counter
        FROM dbo.JournalLines jl
        INNER JOIN CandidateLines c ON c.Id = jl.Id;
    END;

    IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
       AND OBJECT_ID('dbo.TransactionTypes', 'U') IS NOT NULL
    BEGIN
        UPDATE t
           SET TotalBaseAmount     = ROUND(COALESCE(NULLIF(t.TotalCounterAmount, 0), t.TotalAmount, 0) * @CRR_Rate, 4),
               TotalCounterAmount  = COALESCE(NULLIF(t.TotalCounterAmount, 0), t.TotalAmount, 0),
               CounterCurrencyCode = @CRR_Counter,
               BaseCurrencyCode    = @CRR_Base
        FROM dbo.Transactions t
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
        WHERE tt.TypeKey = N'sale'
          AND UPPER(LTRIM(RTRIM(ISNULL(t.BaseCurrencyCode,    N'')))) = @CRR_Base
          AND UPPER(LTRIM(RTRIM(ISNULL(t.CounterCurrencyCode, N'')))) = @CRR_Counter
          AND ISNULL(t.TotalCounterAmount, 0) > 0
          AND ABS(ISNULL(t.TotalBaseAmount, 0) - ISNULL(t.TotalCounterAmount, 0)) <= 0.01;
    END;

    IF OBJECT_ID('dbo.TransactionItems', 'U') IS NOT NULL
       AND OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
       AND OBJECT_ID('dbo.TransactionTypes', 'U') IS NOT NULL
    BEGIN
        UPDATE ti
           SET RateToBase          = @CRR_Rate,
               BaseAmount          = ROUND(COALESCE(NULLIF(ti.CounterAmount, 0), ti.LineTotal, 0) * @CRR_Rate, 4),
               CounterAmount       = COALESCE(NULLIF(ti.CounterAmount, 0), ti.LineTotal, 0),
               CurrencyCode        = @CRR_Counter
        FROM dbo.TransactionItems ti
        INNER JOIN dbo.Transactions t      ON t.Id    = ti.TransactionId
        INNER JOIN dbo.TransactionTypes tt ON tt.Id   = t.TransactionTypeId
        WHERE tt.TypeKey = N'sale'
          AND UPPER(LTRIM(RTRIM(ISNULL(t.BaseCurrencyCode,    N'')))) = @CRR_Base
          AND UPPER(LTRIM(RTRIM(ISNULL(t.CounterCurrencyCode, N'')))) = @CRR_Counter
          AND UPPER(LTRIM(RTRIM(ISNULL(ti.CurrencyCode,       N'')))) = @CRR_Counter
          AND ROUND(ISNULL(ti.RateToBase, 0), 8) = 1
          AND ABS(ISNULL(ti.BaseAmount, 0) - COALESCE(NULLIF(ti.CounterAmount, 0), ti.LineTotal, 0)) <= 0.01;
    END;
END;
GO

