using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class WarrantyClaimsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.WarrantyClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaims
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarrantyClaims PRIMARY KEY,
        ClaimNumber NVARCHAR(50) NOT NULL CONSTRAINT DF_WarrantyClaims_ClaimNumber DEFAULT N'',
        CustomerId INT NULL,
        PartId INT NOT NULL,
        Quantity INT NOT NULL CONSTRAINT DF_WarrantyClaims_Quantity DEFAULT 1,
        ClaimType NVARCHAR(50) NOT NULL CONSTRAINT DF_WarrantyClaims_ClaimType DEFAULT N'Return',
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_WarrantyClaims_Status DEFAULT N'Open',
        Description NVARCHAR(1000) NULL,
        Resolution NVARCHAR(1000) NULL,
        OriginalInvoiceId INT NULL,
        RefundAmount DECIMAL(19,4) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WarrantyClaims_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ResolvedAt DATETIME2(0) NULL,
        ResolvedByUserId INT NULL,
        CONSTRAINT FK_WarrantyClaims_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_WarrantyClaims_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id)
    );
END;
""");
    }
}
