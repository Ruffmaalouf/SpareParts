using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ListingBoostsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.ListingBoosts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListingBoosts
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ListingBoosts PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_ListingBoosts_TenantId DEFAULT 0,
        PartId INT NOT NULL,
        DurationDays INT NOT NULL CONSTRAINT DF_ListingBoosts_DurationDays DEFAULT 7,
        Fee DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_ListingBoosts_Currency DEFAULT 'USD',
        Status INT NOT NULL CONSTRAINT DF_ListingBoosts_Status DEFAULT 1,
        StartsAt DATETIME2 NOT NULL,
        ExpiresAt DATETIME2 NOT NULL,
        PaymentReference NVARCHAR(200) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ListingBoosts_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_ListingBoosts_PartId ON dbo.ListingBoosts (PartId);
    CREATE INDEX IX_ListingBoosts_TenantId ON dbo.ListingBoosts (TenantId);
    CREATE INDEX IX_ListingBoosts_ExpiresAt ON dbo.ListingBoosts (ExpiresAt);
END;
""");
    }
}
