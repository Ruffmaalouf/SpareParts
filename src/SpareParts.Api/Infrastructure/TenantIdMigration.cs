using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds TenantId columns to all tenant-owned tables, backfills existing data to
/// the default tenant (Id=1), and creates covering indexes for performance.
/// Must run after TenantsMigration.
/// </summary>
public static class TenantIdMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();

        // Language: SQL Server — all statements are idempotent.
        conn.Execute("""
-- ── Users ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Users', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Users ADD TenantId INT NULL;
        UPDATE dbo.Users SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_TenantId' AND object_id = OBJECT_ID('dbo.Users'))
        CREATE NONCLUSTERED INDEX IX_Users_TenantId ON dbo.Users (TenantId);
END;

-- ── Parts ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Parts', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts ADD TenantId INT NULL;
        UPDATE dbo.Parts SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Parts_TenantId' AND object_id = OBJECT_ID('dbo.Parts'))
        CREATE NONCLUSTERED INDEX IX_Parts_TenantId ON dbo.Parts (TenantId);
END;

-- ── Stock ─────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Stock', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Stock', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Stock ADD TenantId INT NULL;
        UPDATE dbo.Stock SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Stock_TenantId' AND object_id = OBJECT_ID('dbo.Stock'))
        CREATE NONCLUSTERED INDEX IX_Stock_TenantId ON dbo.Stock (TenantId);
END;

-- ── StockMovements ────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.StockMovements', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.StockMovements ADD TenantId INT NULL;
        UPDATE dbo.StockMovements SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_StockMovements_TenantId' AND object_id = OBJECT_ID('dbo.StockMovements'))
        CREATE NONCLUSTERED INDEX IX_StockMovements_TenantId ON dbo.StockMovements (TenantId);
END;

-- ── Customers ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Customers', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Customers ADD TenantId INT NULL;
        UPDATE dbo.Customers SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Customers_TenantId' AND object_id = OBJECT_ID('dbo.Customers'))
        CREATE NONCLUSTERED INDEX IX_Customers_TenantId ON dbo.Customers (TenantId);
END;

-- ── Suppliers ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Suppliers', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Suppliers', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Suppliers ADD TenantId INT NULL;
        UPDATE dbo.Suppliers SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Suppliers_TenantId' AND object_id = OBJECT_ID('dbo.Suppliers'))
        CREATE NONCLUSTERED INDEX IX_Suppliers_TenantId ON dbo.Suppliers (TenantId);
END;

-- ── Warehouses ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Warehouses', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Warehouses ADD TenantId INT NULL;
        UPDATE dbo.Warehouses SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Warehouses_TenantId' AND object_id = OBJECT_ID('dbo.Warehouses'))
        CREATE NONCLUSTERED INDEX IX_Warehouses_TenantId ON dbo.Warehouses (TenantId);
END;

-- ── Location / Locations ──────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Location', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Location', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Location ADD TenantId INT NULL;
        UPDATE dbo.Location SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.Locations', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Locations', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Locations ADD TenantId INT NULL;
        UPDATE dbo.Locations SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Locations_TenantId' AND object_id = OBJECT_ID('dbo.Locations'))
        CREATE NONCLUSTERED INDEX IX_Locations_TenantId ON dbo.Locations (TenantId);
END;

-- ── Brands ────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Brands', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Brands', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Brands ADD TenantId INT NULL;
        UPDATE dbo.Brands SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Brands_TenantId' AND object_id = OBJECT_ID('dbo.Brands'))
        CREATE NONCLUSTERED INDEX IX_Brands_TenantId ON dbo.Brands (TenantId);
END;

-- ── Categories ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Categories', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Categories ADD TenantId INT NULL;
        UPDATE dbo.Categories SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_TenantId' AND object_id = OBJECT_ID('dbo.Categories'))
        CREATE NONCLUSTERED INDEX IX_Categories_TenantId ON dbo.Categories (TenantId);
END;

-- ── CarBrands / CarModels ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.CarBrands', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.CarBrands', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.CarBrands ADD TenantId INT NULL;
        UPDATE dbo.CarBrands SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.CarModels', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.CarModels', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.CarModels ADD TenantId INT NULL;
        UPDATE dbo.CarModels SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;

-- ── UsedCars ──────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UsedCars', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.UsedCars ADD TenantId INT NULL;
        UPDATE dbo.UsedCars SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsedCars_TenantId' AND object_id = OBJECT_ID('dbo.UsedCars'))
        CREATE NONCLUSTERED INDEX IX_UsedCars_TenantId ON dbo.UsedCars (TenantId);
END;
IF OBJECT_ID('dbo.usedcar_images', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.usedcar_images', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.usedcar_images ADD TenantId INT NULL;
        UPDATE dbo.usedcar_images SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;

-- ── Transactions ──────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Transactions', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Transactions ADD TenantId INT NULL;
        UPDATE dbo.Transactions SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Transactions_TenantId' AND object_id = OBJECT_ID('dbo.Transactions'))
        CREATE NONCLUSTERED INDEX IX_Transactions_TenantId ON dbo.Transactions (TenantId);
END;
IF OBJECT_ID('dbo.TransactionItems', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TransactionItems', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.TransactionItems ADD TenantId INT NULL;
        UPDATE dbo.TransactionItems SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.TransactionTypes', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.TransactionTypes', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.TransactionTypes ADD TenantId INT NULL;
        UPDATE dbo.TransactionTypes SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TransactionTypes_TenantId' AND object_id = OBJECT_ID('dbo.TransactionTypes'))
        CREATE NONCLUSTERED INDEX IX_TransactionTypes_TenantId ON dbo.TransactionTypes (TenantId);
END;

-- ── Accounting ────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Accounts', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Accounts', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Accounts ADD TenantId INT NULL;
        UPDATE dbo.Accounts SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Accounts_TenantId' AND object_id = OBJECT_ID('dbo.Accounts'))
        CREATE NONCLUSTERED INDEX IX_Accounts_TenantId ON dbo.Accounts (TenantId);
END;
IF OBJECT_ID('dbo.AccountingPostingSettings', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AccountingPostingSettings', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.AccountingPostingSettings ADD TenantId INT NULL;
        UPDATE dbo.AccountingPostingSettings SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.JournalEntries', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.JournalEntries', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.JournalEntries ADD TenantId INT NULL;
        UPDATE dbo.JournalEntries SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_JournalEntries_TenantId' AND object_id = OBJECT_ID('dbo.JournalEntries'))
        CREATE NONCLUSTERED INDEX IX_JournalEntries_TenantId ON dbo.JournalEntries (TenantId);
END;
IF OBJECT_ID('dbo.JournalLines', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.JournalLines', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.JournalLines ADD TenantId INT NULL;
        UPDATE dbo.JournalLines SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;

-- ── Purchases ─────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UsedCarPurchases', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.UsedCarPurchases ADD TenantId INT NULL;
        UPDATE dbo.UsedCarPurchases SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsedCarPurchases_TenantId' AND object_id = OBJECT_ID('dbo.UsedCarPurchases'))
        CREATE NONCLUSTERED INDEX IX_UsedCarPurchases_TenantId ON dbo.UsedCarPurchases (TenantId);
END;
IF OBJECT_ID('dbo.UsedCarPurchaseLines', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UsedCarPurchaseLines', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.UsedCarPurchaseLines ADD TenantId INT NULL;
        UPDATE dbo.UsedCarPurchaseLines SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.UsedCarWholesaleSales', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UsedCarWholesaleSales', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.UsedCarWholesaleSales ADD TenantId INT NULL;
        UPDATE dbo.UsedCarWholesaleSales SET TenantId = 1 WHERE TenantId IS NULL;
    END;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UsedCarWholesaleSales_TenantId' AND object_id = OBJECT_ID('dbo.UsedCarWholesaleSales'))
        CREATE NONCLUSTERED INDEX IX_UsedCarWholesaleSales_TenantId ON dbo.UsedCarWholesaleSales (TenantId);
END;

-- ── Quotes ────────────────────────────────────────────────────────────────────
IF OBJECT_ID('dbo.Quotes', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Quotes', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Quotes ADD TenantId INT NULL;
        UPDATE dbo.Quotes SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.QuoteItems', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.QuoteItems', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.QuoteItems ADD TenantId INT NULL;
        UPDATE dbo.QuoteItems SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;

-- ── Other business tables ─────────────────────────────────────────────────────
IF OBJECT_ID('dbo.PartRequests', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PartRequests', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.PartRequests ADD TenantId INT NULL;
        UPDATE dbo.PartRequests SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.PartRequestReservations', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PartRequestReservations', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.PartRequestReservations ADD TenantId INT NULL;
        UPDATE dbo.PartRequestReservations SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.PartSubstitutes', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PartSubstitutes', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.PartSubstitutes ADD TenantId INT NULL;
        UPDATE dbo.PartSubstitutes SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.ReorderRules', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ReorderRules', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.ReorderRules ADD TenantId INT NULL;
        UPDATE dbo.ReorderRules SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.OutboundMessages', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.OutboundMessages', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.OutboundMessages ADD TenantId INT NULL;
        UPDATE dbo.OutboundMessages SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.WhatsAppCampaigns', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.WhatsAppCampaigns', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.WhatsAppCampaigns ADD TenantId INT NULL;
        UPDATE dbo.WhatsAppCampaigns SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.WhatsAppCampaignRecipients', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.WhatsAppCampaignRecipients ADD TenantId INT NULL;
        UPDATE dbo.WhatsAppCampaignRecipients SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.ReportBuilderSavedReports', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ReportBuilderSavedReports', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.ReportBuilderSavedReports ADD TenantId INT NULL;
        UPDATE dbo.ReportBuilderSavedReports SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.ReportBuilderFavoriteReports', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ReportBuilderFavoriteReports', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.ReportBuilderFavoriteReports ADD TenantId INT NULL;
        UPDATE dbo.ReportBuilderFavoriteReports SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.ReportBuilderTableLinks', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ReportBuilderTableLinks', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.ReportBuilderTableLinks ADD TenantId INT NULL;
        UPDATE dbo.ReportBuilderTableLinks SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.Shipments', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Shipments', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.Shipments ADD TenantId INT NULL;
        UPDATE dbo.Shipments SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.LoyaltyTransactions', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.LoyaltyTransactions', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.LoyaltyTransactions ADD TenantId INT NULL;
        UPDATE dbo.LoyaltyTransactions SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.WarrantyClaims', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.WarrantyClaims', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.WarrantyClaims ADD TenantId INT NULL;
        UPDATE dbo.WarrantyClaims SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.SupplierPriceHistory', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SupplierPriceHistory', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.SupplierPriceHistory ADD TenantId INT NULL;
        UPDATE dbo.SupplierPriceHistory SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.ActivityLogs', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ActivityLogs', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.ActivityLogs ADD TenantId INT NULL;
        UPDATE dbo.ActivityLogs SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.CurrencyRates', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.CurrencyRates', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.CurrencyRates ADD TenantId INT NULL;
        UPDATE dbo.CurrencyRates SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.AppConstants', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.AppConstants', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.AppConstants ADD TenantId INT NULL;
        UPDATE dbo.AppConstants SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
IF OBJECT_ID('dbo.ExcelImportMetadata', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ExcelImportMetadata', 'TenantId') IS NULL
    BEGIN
        ALTER TABLE dbo.ExcelImportMetadata ADD TenantId INT NULL;
        UPDATE dbo.ExcelImportMetadata SET TenantId = 1 WHERE TenantId IS NULL;
    END;
END;
""",
            commandTimeout: 120);
    }
}
