using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class InvoiceNumberingMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
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
END;");
    }
}
