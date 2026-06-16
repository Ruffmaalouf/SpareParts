using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class EscrowTransactionsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.EscrowTransactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.EscrowTransactions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EscrowTransactions PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_EscrowTransactions_TenantId DEFAULT 0,
        SalesInvoiceId INT NULL,
        PartId INT NULL,
        BuyerName NVARCHAR(120) NOT NULL,
        BuyerPhone NVARCHAR(40) NULL,
        SellerName NVARCHAR(120) NOT NULL,
        SellerPhone NVARCHAR(40) NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_EscrowTransactions_Currency DEFAULT 'USD',
        Status INT NOT NULL CONSTRAINT DF_EscrowTransactions_Status DEFAULT 1,
        Notes NVARCHAR(1000) NULL,
        TrackingNumber NVARCHAR(100) NULL,
        ShippedAt DATETIME2 NULL,
        DeliveredAt DATETIME2 NULL,
        ReleasedAt DATETIME2 NULL,
        DisputedAt DATETIME2 NULL,
        DisputeReason NVARCHAR(500) NULL,
        AutoReleaseAfterHours INT NOT NULL CONSTRAINT DF_EscrowTransactions_AutoReleaseAfterHours DEFAULT 48,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_EscrowTransactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_EscrowTransactions_TenantId ON dbo.EscrowTransactions (TenantId);
    CREATE INDEX IX_EscrowTransactions_Status ON dbo.EscrowTransactions (Status);
END;
""");
    }
}
