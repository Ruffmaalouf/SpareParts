-- ════════════════════════════════════════════════════════════════════════════
-- Ignition Redesign — Phase 0 — Covering Indexes
-- Source spec : docs/redesign/04-system-architecture.html §07 ("Covering
--               indexes required") · docs/redesign/06-consolidated-overview.html
--               Phase 0 (dev-database) · Risk R8.
-- Author      : dev-database
-- Date        : 2026-07-03
--
-- ⚠ APPROVAL REQUIRED BEFORE EXECUTION ⚠
-- Per CLAUDE.md workspace rules, this script must NOT be run against any
-- shared, staging, or production database without Ralph's explicit approval
-- in the current conversation, and only during a low-traffic window
-- (dbo.Transactions can be large; index creation takes schema locks on
-- Standard Edition where ONLINE builds are unavailable).
--
-- WHAT THIS SCRIPT DOES
--   Creates the four covering indexes from Report 04 §07:
--     1. IX_Transactions_Tenant_Type_Date  on dbo.Transactions
--     2. IX_Transactions_Tenant_Customer   on dbo.Transactions
--     3. IX_Stock_Tenant_Part              on dbo.Stock
--     4. IX_Customers_Tenant_Name          on dbo.Customers
--
-- SAFETY PROPERTIES
--   • Additive and index-only — zero data change, no table/column DDL.
--   • Idempotent — every CREATE INDEX is guarded by a sys.indexes lookup;
--     the script is safe to re-run.
--   • ONLINE-friendly — uses ONLINE = ON automatically when the engine
--     edition supports it (Enterprise/Developer = 3, Azure SQL DB = 5,
--     Azure Managed Instance = 8); falls back to an offline build otherwise.
--   • Column-guarded — the TenantId columns are added at runtime by
--     src/SpareParts.Api/Infrastructure/TenantIdMigration.cs (they are NOT
--     in database/schema.sql). If TenantId is missing on a target table,
--     that index is skipped with a message instead of failing, so this
--     script can be deployed ahead of code per Report 04 §11 step 1 —
--     but only against a database whose API migrations have run at least
--     once (otherwise re-run this script after the first API startup).
--
-- ROLLBACK
--   Each index can be dropped independently (DROP INDEX <name> ON <table>).
--   Drops are intentionally NOT included here: destructive statements
--   require separate explicit approval per CLAUDE.md.
--
-- NOTE ON DYNAMIC SQL
--   CREATE INDEX cannot take the ONLINE option as a runtime parameter, so
--   each statement is assembled from CONSTANT literals in this file plus the
--   edition-derived @WithOptions fragment and run via sp_executesql. No user
--   or external input is ever concatenated — this keeps the script
--   compatible with the workspace parameterized-SQL rule (which governs
--   values, not edition-conditional DDL options).
-- ════════════════════════════════════════════════════════════════════════════

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @WithOptions NVARCHAR(200) =
    CASE WHEN CONVERT(INT, SERVERPROPERTY('EngineEdition')) IN (3, 5, 8)
         THEN N' WITH (ONLINE = ON, SORT_IN_TEMPDB = ON)'
         ELSE N''
    END;
DECLARE @Sql NVARCHAR(MAX);

IF @WithOptions = N''
    PRINT 'Engine edition does not support ONLINE index builds — indexes will be created offline. Run during a low-traffic window.';

-- ────────────────────────────────────────────────────────────────────────────
-- 1. IX_Transactions_Tenant_Type_Date
--    Serves: dashboard trend series, daily summary, unpaid-transactions
--    panel — all filter by tenant + transaction type + date range.
--    (Report 04 §07, row 1)
-- ────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
    PRINT 'SKIPPED IX_Transactions_Tenant_Type_Date — dbo.Transactions does not exist.';
ELSE IF COL_LENGTH('dbo.Transactions', 'TenantId') IS NULL
    PRINT 'SKIPPED IX_Transactions_Tenant_Type_Date — dbo.Transactions.TenantId does not exist yet. Run the API once (TenantIdMigration) and re-run this script.';
ELSE IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'IX_Transactions_Tenant_Type_Date')
    PRINT 'EXISTS  IX_Transactions_Tenant_Type_Date — nothing to do.';
ELSE
BEGIN
    SET @Sql = N'CREATE NONCLUSTERED INDEX IX_Transactions_Tenant_Type_Date
        ON dbo.Transactions (TenantId, TransactionTypeId, TransactionDate)
        INCLUDE (TotalAmount, PaidAmount, TotalCounterAmount, PaidCounterAmount, IsReturn, CustomerId)'
        + @WithOptions + N';';
    EXEC sp_executesql @Sql;
    PRINT 'CREATED IX_Transactions_Tenant_Type_Date';
END;

-- ────────────────────────────────────────────────────────────────────────────
-- 2. IX_Transactions_Tenant_Customer
--    Serves: per-customer aging query (client workspace) and workspace
--    invoices/payments tab filters. (Report 04 §07, row 2)
-- ────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
    PRINT 'SKIPPED IX_Transactions_Tenant_Customer — dbo.Transactions does not exist.';
ELSE IF COL_LENGTH('dbo.Transactions', 'TenantId') IS NULL
    PRINT 'SKIPPED IX_Transactions_Tenant_Customer — dbo.Transactions.TenantId does not exist yet. Run the API once (TenantIdMigration) and re-run this script.';
ELSE IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'IX_Transactions_Tenant_Customer')
    PRINT 'EXISTS  IX_Transactions_Tenant_Customer — nothing to do.';
ELSE
BEGIN
    SET @Sql = N'CREATE NONCLUSTERED INDEX IX_Transactions_Tenant_Customer
        ON dbo.Transactions (TenantId, CustomerId)
        INCLUDE (TotalAmount, PaidAmount, TransactionDate)'
        + @WithOptions + N';';
    EXEC sp_executesql @Sql;
    PRINT 'CREATED IX_Transactions_Tenant_Customer';
END;

-- ────────────────────────────────────────────────────────────────────────────
-- 3. IX_Stock_Tenant_Part
--    Serves: stock-value and low-stock aggregates. Report 04 §07 flags this
--    one as "verify it exists before shipping" — the guard below does that.
--    (Report 04 §07, row 3)
-- ────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Stock', 'U') IS NULL
    PRINT 'SKIPPED IX_Stock_Tenant_Part — dbo.Stock does not exist.';
ELSE IF COL_LENGTH('dbo.Stock', 'TenantId') IS NULL
    PRINT 'SKIPPED IX_Stock_Tenant_Part — dbo.Stock.TenantId does not exist yet. Run the API once (TenantIdMigration) and re-run this script.';
ELSE IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Stock')
      AND name = 'IX_Stock_Tenant_Part')
    PRINT 'EXISTS  IX_Stock_Tenant_Part — nothing to do.';
ELSE
BEGIN
    SET @Sql = N'CREATE NONCLUSTERED INDEX IX_Stock_Tenant_Part
        ON dbo.Stock (TenantId, PartId)
        INCLUDE (Quantity)'
        + @WithOptions + N';';
    EXEC sp_executesql @Sql;
    PRINT 'CREATED IX_Stock_Tenant_Part';
END;

-- ────────────────────────────────────────────────────────────────────────────
-- 4. IX_Customers_Tenant_Name
--    Serves: client-rail typeahead search prefix scans
--    (GET /api/clients/search). (Report 04 §07, row 4)
-- ────────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Customers', 'U') IS NULL
    PRINT 'SKIPPED IX_Customers_Tenant_Name — dbo.Customers does not exist.';
ELSE IF COL_LENGTH('dbo.Customers', 'TenantId') IS NULL
    PRINT 'SKIPPED IX_Customers_Tenant_Name — dbo.Customers.TenantId does not exist yet. Run the API once (TenantIdMigration) and re-run this script.';
ELSE IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Customers')
      AND name = 'IX_Customers_Tenant_Name')
    PRINT 'EXISTS  IX_Customers_Tenant_Name — nothing to do.';
ELSE
BEGIN
    SET @Sql = N'CREATE NONCLUSTERED INDEX IX_Customers_Tenant_Name
        ON dbo.Customers (TenantId, Name)
        INCLUDE (Phone)'
        + @WithOptions + N';';
    EXEC sp_executesql @Sql;
    PRINT 'CREATED IX_Customers_Tenant_Name';
END;
GO

-- ────────────────────────────────────────────────────────────────────────────
-- Post-run verification (read-only). Expected: 4 rows after a full run on a
-- database whose TenantIdMigration has been applied.
-- ────────────────────────────────────────────────────────────────────────────
SELECT
    OBJECT_SCHEMA_NAME(i.object_id) + N'.' + OBJECT_NAME(i.object_id) AS TableName,
    i.name      AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
WHERE i.name IN (
    N'IX_Transactions_Tenant_Type_Date',
    N'IX_Transactions_Tenant_Customer',
    N'IX_Stock_Tenant_Part',
    N'IX_Customers_Tenant_Name')
ORDER BY TableName, IndexName;
GO
