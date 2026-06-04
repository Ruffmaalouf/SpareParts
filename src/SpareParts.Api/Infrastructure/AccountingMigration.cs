using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class AccountingMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
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
    WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(CONVERT(NVARCHAR(50), AccountType))), ' ', ''), '_', ''), '-', '')) IN ('INCOME', 'REVENUE', 'REVENUES', 'SALE', 'SALES', 'SALESREVENUE') THEN 'income'
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
END;");
    }
}
