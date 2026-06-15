using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Creates the pricing packages / subscriptions / payments / invoices schema and seeds the
/// FREE, BASIC, PRO and ENTERPRISE packages with their default features and limits.
/// PackageLimits.LimitValue of -1 means "unlimited".
/// </summary>
public static class PricingPackagesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.PricingPackages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PricingPackages
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PricingPackages PRIMARY KEY,
        Code NVARCHAR(40) NOT NULL,
        Name NVARCHAR(120) NOT NULL,
        Description NVARCHAR(500) NOT NULL CONSTRAINT DF_PricingPackages_Description DEFAULT N'',
        MonthlyPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_PricingPackages_MonthlyPrice DEFAULT 0,
        YearlyPrice DECIMAL(18,2) NOT NULL CONSTRAINT DF_PricingPackages_YearlyPrice DEFAULT 0,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_PricingPackages_Currency DEFAULT N'USD',
        SortOrder INT NOT NULL CONSTRAINT DF_PricingPackages_SortOrder DEFAULT 0,
        IsActive BIT NOT NULL CONSTRAINT DF_PricingPackages_IsActive DEFAULT 1,
        IsCustomPricing BIT NOT NULL CONSTRAINT DF_PricingPackages_IsCustomPricing DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PricingPackages_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT UQ_PricingPackages_Code UNIQUE (Code)
    );
END;

IF OBJECT_ID('dbo.PackageFeatures', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageFeatures
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PackageFeatures PRIMARY KEY,
        PackageId INT NOT NULL,
        FeatureCode NVARCHAR(60) NOT NULL,
        IsEnabled BIT NOT NULL CONSTRAINT DF_PackageFeatures_IsEnabled DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PackageFeatures_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_PackageFeatures_Packages FOREIGN KEY (PackageId) REFERENCES dbo.PricingPackages (Id),
        CONSTRAINT UQ_PackageFeatures_Package_Feature UNIQUE (PackageId, FeatureCode)
    );
    CREATE INDEX IX_PackageFeatures_PackageId ON dbo.PackageFeatures (PackageId);
END;

IF OBJECT_ID('dbo.PackageLimits', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PackageLimits
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PackageLimits PRIMARY KEY,
        PackageId INT NOT NULL,
        LimitCode NVARCHAR(60) NOT NULL,
        LimitValue INT NOT NULL CONSTRAINT DF_PackageLimits_LimitValue DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PackageLimits_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_PackageLimits_Packages FOREIGN KEY (PackageId) REFERENCES dbo.PricingPackages (Id),
        CONSTRAINT UQ_PackageLimits_Package_Limit UNIQUE (PackageId, LimitCode)
    );
    CREATE INDEX IX_PackageLimits_PackageId ON dbo.PackageLimits (PackageId);
END;

IF OBJECT_ID('dbo.TenantSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantSubscriptions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TenantSubscriptions PRIMARY KEY,
        TenantId INT NOT NULL,
        PackageId INT NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_TenantSubscriptions_Status DEFAULT 1,
        BillingCycle INT NOT NULL CONSTRAINT DF_TenantSubscriptions_BillingCycle DEFAULT 1,
        CurrentPeriodStart DATETIME2(0) NOT NULL CONSTRAINT DF_TenantSubscriptions_PeriodStart DEFAULT SYSUTCDATETIME(),
        CurrentPeriodEnd DATETIME2(0) NOT NULL,
        TrialEndsAt DATETIME2(0) NULL,
        CancelledAt DATETIME2(0) NULL,
        CancelAtPeriodEnd BIT NOT NULL CONSTRAINT DF_TenantSubscriptions_CancelAtPeriodEnd DEFAULT 0,
        ProviderCode NVARCHAR(40) NULL,
        ProviderSubscriptionId NVARCHAR(120) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TenantSubscriptions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_TenantSubscriptions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT FK_TenantSubscriptions_Packages FOREIGN KEY (PackageId) REFERENCES dbo.PricingPackages (Id)
    );
    CREATE INDEX IX_TenantSubscriptions_TenantId ON dbo.TenantSubscriptions (TenantId);
    CREATE INDEX IX_TenantSubscriptions_PackageId ON dbo.TenantSubscriptions (PackageId);
    CREATE INDEX IX_TenantSubscriptions_ProviderSubscriptionId ON dbo.TenantSubscriptions (ProviderSubscriptionId);
END;

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Payments PRIMARY KEY,
        TenantId INT NOT NULL,
        SubscriptionId INT NULL,
        ProviderCode NVARCHAR(40) NOT NULL,
        ProviderPaymentId NVARCHAR(120) NULL,
        ProviderCheckoutSessionId NVARCHAR(120) NULL,
        PackageCode NVARCHAR(40) NOT NULL,
        BillingCycle INT NOT NULL CONSTRAINT DF_Payments_BillingCycle DEFAULT 1,
        Amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Payments_Amount DEFAULT 0,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_Payments_Currency DEFAULT N'USD',
        Status INT NOT NULL CONSTRAINT DF_Payments_Status DEFAULT 1,
        Notes NVARCHAR(1000) NULL,
        PaidAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Payments_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Payments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT FK_Payments_Subscriptions FOREIGN KEY (SubscriptionId) REFERENCES dbo.TenantSubscriptions (Id)
    );
    CREATE INDEX IX_Payments_TenantId ON dbo.Payments (TenantId);
    CREATE INDEX IX_Payments_SubscriptionId ON dbo.Payments (SubscriptionId);
    CREATE INDEX IX_Payments_ProviderPaymentId ON dbo.Payments (ProviderPaymentId);
    CREATE INDEX IX_Payments_ProviderCheckoutSessionId ON dbo.Payments (ProviderCheckoutSessionId);
END;

IF OBJECT_ID('dbo.Invoices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
        TenantId INT NOT NULL,
        SubscriptionId INT NULL,
        PaymentId INT NULL,
        InvoiceNumber NVARCHAR(40) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_Subtotal DEFAULT 0,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_TaxAmount DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_TotalAmount DEFAULT 0,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_Invoices_Currency DEFAULT N'USD',
        Status INT NOT NULL CONSTRAINT DF_Invoices_Status DEFAULT 1,
        IssuedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_IssuedAt DEFAULT SYSUTCDATETIME(),
        DueAt DATETIME2(0) NULL,
        PaidAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Invoices_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT FK_Invoices_Subscriptions FOREIGN KEY (SubscriptionId) REFERENCES dbo.TenantSubscriptions (Id),
        CONSTRAINT FK_Invoices_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (Id),
        CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE (InvoiceNumber)
    );
    CREATE INDEX IX_Invoices_TenantId ON dbo.Invoices (TenantId);
    CREATE INDEX IX_Invoices_SubscriptionId ON dbo.Invoices (SubscriptionId);
    CREATE INDEX IX_Invoices_PaymentId ON dbo.Invoices (PaymentId);
END;

IF OBJECT_ID('dbo.PaymentTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentTransactions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentTransactions PRIMARY KEY,
        PaymentId INT NOT NULL,
        ProviderCode NVARCHAR(40) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_PaymentTransactions_Status DEFAULT 1,
        RawPayload NVARCHAR(MAX) NULL,
        Message NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PaymentTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_PaymentTransactions_Payments FOREIGN KEY (PaymentId) REFERENCES dbo.Payments (Id)
    );
    CREATE INDEX IX_PaymentTransactions_PaymentId ON dbo.PaymentTransactions (PaymentId);
END;

IF OBJECT_ID('dbo.WebhookEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WebhookEvents
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebhookEvents PRIMARY KEY,
        Provider NVARCHAR(40) NOT NULL,
        EventId NVARCHAR(160) NOT NULL,
        EventType NVARCHAR(100) NOT NULL,
        Payload NVARCHAR(MAX) NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_WebhookEvents_Status DEFAULT 1,
        ProcessedAt DATETIME2(0) NULL,
        Error NVARCHAR(1000) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WebhookEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT UQ_WebhookEvents_Provider_EventId UNIQUE (Provider, EventId)
    );
END;

IF OBJECT_ID('dbo.TenantUsageCounters', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantUsageCounters
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TenantUsageCounters PRIMARY KEY,
        TenantId INT NOT NULL,
        CounterCode NVARCHAR(60) NOT NULL,
        PeriodYear INT NOT NULL,
        PeriodMonth INT NOT NULL,
        CurrentValue INT NOT NULL CONSTRAINT DF_TenantUsageCounters_CurrentValue DEFAULT 0,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TenantUsageCounters_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_TenantUsageCounters_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        CONSTRAINT UQ_TenantUsageCounters_Tenant_Counter_Period UNIQUE (TenantId, CounterCode, PeriodYear, PeriodMonth)
    );
    CREATE INDEX IX_TenantUsageCounters_TenantId ON dbo.TenantUsageCounters (TenantId);
END;

IF COL_LENGTH('dbo.Parts', 'ImageUrls') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD ImageUrls NVARCHAR(MAX) NULL;
END;

IF COL_LENGTH('dbo.Parts', 'IsPromoted') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD IsPromoted BIT NOT NULL CONSTRAINT DF_Parts_IsPromoted DEFAULT 0;
END;

IF COL_LENGTH('dbo.Parts', 'IsFeatured') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD IsFeatured BIT NOT NULL CONSTRAINT DF_Parts_IsFeatured DEFAULT 0;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PricingPackages WHERE Code = 'FREE')
BEGIN
    INSERT INTO dbo.PricingPackages (Code, Name, Description, MonthlyPrice, YearlyPrice, Currency, SortOrder, IsActive, IsCustomPricing, CreatedAt)
    VALUES ('FREE', N'Free', N'Get started and list your first parts at no cost.', 0, 0, 'USD', 1, 1, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PricingPackages WHERE Code = 'BASIC')
BEGIN
    INSERT INTO dbo.PricingPackages (Code, Name, Description, MonthlyPrice, YearlyPrice, Currency, SortOrder, IsActive, IsCustomPricing, CreatedAt)
    VALUES ('BASIC', N'Basic', N'For small sellers ready to grow with mobile access and reports.', 19, 190, 'USD', 2, 1, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PricingPackages WHERE Code = 'PRO')
BEGIN
    INSERT INTO dbo.PricingPackages (Code, Name, Description, MonthlyPrice, YearlyPrice, Currency, SortOrder, IsActive, IsCustomPricing, CreatedAt)
    VALUES ('PRO', N'Pro', N'For growing sellers who need AI tools, multiple branches and promoted listings.', 49, 490, 'USD', 3, 1, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PricingPackages WHERE Code = 'ENTERPRISE')
BEGIN
    INSERT INTO dbo.PricingPackages (Code, Name, Description, MonthlyPrice, YearlyPrice, Currency, SortOrder, IsActive, IsCustomPricing, CreatedAt)
    VALUES ('ENTERPRISE', N'Enterprise', N'Custom limits, API access, white-label and priority support for large operations.', 0, 0, 'USD', 4, 1, 1, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PackageFeatures pf INNER JOIN dbo.PricingPackages pp ON pp.Id = pf.PackageId WHERE pp.Code = 'FREE')
BEGIN
    DECLARE @FreePackageId INT = (SELECT Id FROM dbo.PricingPackages WHERE Code = 'FREE');

    INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
    SELECT @FreePackageId, v.FeatureCode, v.IsEnabled, SYSUTCDATETIME()
    FROM (VALUES
        ('PART_LISTING', 1),
        ('PART_IMAGES', 1),
        ('BUYER_LEADS', 1),
        ('MOBILE_SELLER_APP', 0),
        ('REPORTS_BASIC', 0),
        ('REPORTS_ADVANCED', 0),
        ('MULTI_BRANCH', 0),
        ('BULK_IMPORT', 0),
        ('AI_TITLE', 0),
        ('AI_DESCRIPTION', 0),
        ('AI_PRICE_SUGGESTION', 0),
        ('PROMOTED_LISTINGS', 0),
        ('FEATURED_PARTS', 0),
        ('API_ACCESS', 0),
        ('PRIORITY_SUPPORT', 0),
        ('CUSTOM_INTEGRATIONS', 0),
        ('WHITE_LABEL', 0)
    ) AS v(FeatureCode, IsEnabled);

    INSERT INTO dbo.PackageLimits (PackageId, LimitCode, LimitValue, CreatedAt)
    SELECT @FreePackageId, v.LimitCode, v.LimitValue, SYSUTCDATETIME()
    FROM (VALUES
        ('USERS_COUNT', 1),
        ('BRANCHES_COUNT', 1),
        ('ACTIVE_PARTS_COUNT', 50),
        ('IMAGES_PER_PART', 3),
        ('BUYER_LEADS_MONTHLY', 10),
        ('PROMOTED_PARTS_MONTHLY', 0),
        ('FEATURED_PARTS_MONTHLY', 0),
        ('API_CALLS_MONTHLY', 0)
    ) AS v(LimitCode, LimitValue);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PackageFeatures pf INNER JOIN dbo.PricingPackages pp ON pp.Id = pf.PackageId WHERE pp.Code = 'BASIC')
BEGIN
    DECLARE @BasicPackageId INT = (SELECT Id FROM dbo.PricingPackages WHERE Code = 'BASIC');

    INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
    SELECT @BasicPackageId, v.FeatureCode, v.IsEnabled, SYSUTCDATETIME()
    FROM (VALUES
        ('PART_LISTING', 1),
        ('PART_IMAGES', 1),
        ('BUYER_LEADS', 1),
        ('MOBILE_SELLER_APP', 1),
        ('REPORTS_BASIC', 1),
        ('REPORTS_ADVANCED', 0),
        ('MULTI_BRANCH', 0),
        ('BULK_IMPORT', 0),
        ('AI_TITLE', 0),
        ('AI_DESCRIPTION', 0),
        ('AI_PRICE_SUGGESTION', 0),
        ('PROMOTED_LISTINGS', 0),
        ('FEATURED_PARTS', 0),
        ('API_ACCESS', 0),
        ('PRIORITY_SUPPORT', 0),
        ('CUSTOM_INTEGRATIONS', 0),
        ('WHITE_LABEL', 0)
    ) AS v(FeatureCode, IsEnabled);

    INSERT INTO dbo.PackageLimits (PackageId, LimitCode, LimitValue, CreatedAt)
    SELECT @BasicPackageId, v.LimitCode, v.LimitValue, SYSUTCDATETIME()
    FROM (VALUES
        ('USERS_COUNT', 3),
        ('BRANCHES_COUNT', 1),
        ('ACTIVE_PARTS_COUNT', 500),
        ('IMAGES_PER_PART', 8),
        ('BUYER_LEADS_MONTHLY', 100),
        ('PROMOTED_PARTS_MONTHLY', 0),
        ('FEATURED_PARTS_MONTHLY', 0),
        ('API_CALLS_MONTHLY', 0)
    ) AS v(LimitCode, LimitValue);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PackageFeatures pf INNER JOIN dbo.PricingPackages pp ON pp.Id = pf.PackageId WHERE pp.Code = 'PRO')
BEGIN
    DECLARE @ProPackageId INT = (SELECT Id FROM dbo.PricingPackages WHERE Code = 'PRO');

    INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
    SELECT @ProPackageId, v.FeatureCode, v.IsEnabled, SYSUTCDATETIME()
    FROM (VALUES
        ('PART_LISTING', 1),
        ('PART_IMAGES', 1),
        ('BUYER_LEADS', 1),
        ('MOBILE_SELLER_APP', 1),
        ('REPORTS_BASIC', 1),
        ('REPORTS_ADVANCED', 1),
        ('MULTI_BRANCH', 1),
        ('BULK_IMPORT', 1),
        ('AI_TITLE', 1),
        ('AI_DESCRIPTION', 1),
        ('AI_PRICE_SUGGESTION', 1),
        ('PROMOTED_LISTINGS', 1),
        ('FEATURED_PARTS', 1),
        ('API_ACCESS', 0),
        ('PRIORITY_SUPPORT', 1),
        ('CUSTOM_INTEGRATIONS', 1),
        ('WHITE_LABEL', 1)
    ) AS v(FeatureCode, IsEnabled);

    INSERT INTO dbo.PackageLimits (PackageId, LimitCode, LimitValue, CreatedAt)
    SELECT @ProPackageId, v.LimitCode, v.LimitValue, SYSUTCDATETIME()
    FROM (VALUES
        ('USERS_COUNT', 10),
        ('BRANCHES_COUNT', 3),
        ('ACTIVE_PARTS_COUNT', 5000),
        ('IMAGES_PER_PART', 15),
        ('BUYER_LEADS_MONTHLY', -1),
        ('PROMOTED_PARTS_MONTHLY', 20),
        ('FEATURED_PARTS_MONTHLY', 10),
        ('API_CALLS_MONTHLY', 0)
    ) AS v(LimitCode, LimitValue);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.PackageFeatures pf INNER JOIN dbo.PricingPackages pp ON pp.Id = pf.PackageId WHERE pp.Code = 'ENTERPRISE')
BEGIN
    DECLARE @EnterprisePackageId INT = (SELECT Id FROM dbo.PricingPackages WHERE Code = 'ENTERPRISE');

    INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
    SELECT @EnterprisePackageId, v.FeatureCode, v.IsEnabled, SYSUTCDATETIME()
    FROM (VALUES
        ('PART_LISTING', 1),
        ('PART_IMAGES', 1),
        ('BUYER_LEADS', 1),
        ('MOBILE_SELLER_APP', 1),
        ('REPORTS_BASIC', 1),
        ('REPORTS_ADVANCED', 1),
        ('MULTI_BRANCH', 1),
        ('BULK_IMPORT', 1),
        ('AI_TITLE', 1),
        ('AI_DESCRIPTION', 1),
        ('AI_PRICE_SUGGESTION', 1),
        ('PROMOTED_LISTINGS', 1),
        ('FEATURED_PARTS', 1),
        ('API_ACCESS', 1),
        ('PRIORITY_SUPPORT', 1),
        ('CUSTOM_INTEGRATIONS', 1),
        ('WHITE_LABEL', 1)
    ) AS v(FeatureCode, IsEnabled);

    INSERT INTO dbo.PackageLimits (PackageId, LimitCode, LimitValue, CreatedAt)
    SELECT @EnterprisePackageId, v.LimitCode, v.LimitValue, SYSUTCDATETIME()
    FROM (VALUES
        ('USERS_COUNT', -1),
        ('BRANCHES_COUNT', -1),
        ('ACTIVE_PARTS_COUNT', -1),
        ('IMAGES_PER_PART', -1),
        ('BUYER_LEADS_MONTHLY', -1),
        ('PROMOTED_PARTS_MONTHLY', -1),
        ('FEATURED_PARTS_MONTHLY', -1),
        ('API_CALLS_MONTHLY', -1)
    ) AS v(LimitCode, LimitValue);
END;
""");
    }
}
