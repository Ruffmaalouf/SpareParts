using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class InsuranceAddonsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.InsuranceOptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InsuranceOptions
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceOptions PRIMARY KEY,
        Name                NVARCHAR(200) NOT NULL,
        Description         NVARCHAR(500) NULL,
        PricePercent        DECIMAL(5,2) NOT NULL,
        MaxCoverageAmount   DECIMAL(18,4) NULL,
        Currency            NVARCHAR(10) NOT NULL CONSTRAINT DF_InsuranceOptions_Currency DEFAULT 'USD',
        IsActive            BIT NOT NULL CONSTRAINT DF_InsuranceOptions_IsActive DEFAULT 1
    );
    INSERT INTO dbo.InsuranceOptions (Name, Description, PricePercent, MaxCoverageAmount, Currency)
    VALUES
        ('Basic Protection', '30-day parts guarantee. Wrong part or DOA covered.', 2.5, 500, 'USD'),
        ('Full Coverage', '90-day comprehensive coverage including condition disputes.', 5.0, 2000, 'USD');
END;
IF OBJECT_ID('dbo.InsuranceAddons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InsuranceAddons
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsuranceAddons PRIMARY KEY,
        TenantId            INT NOT NULL CONSTRAINT DF_InsuranceAddons_TenantId DEFAULT 0,
        SalesInvoiceId      INT NULL,
        PartId              INT NULL,
        InsuranceOptionId   INT NOT NULL,
        Price               DECIMAL(18,4) NOT NULL,
        Currency            NVARCHAR(10) NOT NULL CONSTRAINT DF_InsuranceAddons_Currency DEFAULT 'USD',
        CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_InsuranceAddons_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId     INT NULL
    );
END;
""");
    }
}
