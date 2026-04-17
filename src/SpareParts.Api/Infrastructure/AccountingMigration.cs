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
        ('purchase_offset', 'Purchase Offset', 'Credited when a purchase invoice is posted.', 50, 1)
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

MERGE dbo.AccountingPostingSettings AS target
USING
(
    VALUES
        ('sales_cash', 1),
        ('sales_revenue', 5),
        ('cogs', 6),
        ('inventory', 2),
        ('purchase_offset', 4)
) AS source (SettingKey, AccountId)
ON target.SettingKey = source.SettingKey
WHEN NOT MATCHED THEN
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
