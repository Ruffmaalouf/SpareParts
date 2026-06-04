-- SpareParts Database Schema
-- Generated for staging deployment

SET NOCOUNT ON;
SET XACT_ABORT ON;
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
    UpdatedAt     DATETIME2     NOT NULL
);
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
    ShippingFeesCurrencyCode   NVARCHAR(10)  NOT NULL DEFAULT 'USD',
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
    AccountTypeKey    NVARCHAR(50)  NULL,
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
    TypeKey     NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Label       NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1
);
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
    RoleKey     NVARCHAR(50)  NOT NULL PRIMARY KEY,
    Label       NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    SortOrder   INT           NOT NULL DEFAULT 0,
    IsActive    BIT           NOT NULL DEFAULT 1
);
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

-- Pre-create FK so AccountingMigration cannot resize AccountTypeKey to NVARCHAR(40)
-- (migration expects NVARCHAR(40) on both sides; schema creates NVARCHAR(50);
--  adding the FK here causes the migration's ALTER COLUMN to fail silently,
--  keeping both columns at NVARCHAR(50) and matching each other for the FK.)
IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.AccountingAccountTypes', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Accounts_AccountTypeKey')
    ALTER TABLE dbo.Accounts WITH NOCHECK
    ADD CONSTRAINT FK_Accounts_AccountTypeKey
        FOREIGN KEY (AccountTypeKey) REFERENCES dbo.AccountingAccountTypes(TypeKey);
GO
