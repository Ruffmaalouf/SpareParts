using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ReferralsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.ReferralCodes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReferralCodes
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReferralCodes PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_ReferralCodes_TenantId DEFAULT 0,
        UserId INT NOT NULL,
        Code NVARCHAR(20) NOT NULL CONSTRAINT UQ_ReferralCodes_Code UNIQUE,
        TotalReferrals INT NOT NULL CONSTRAINT DF_ReferralCodes_TotalReferrals DEFAULT 0,
        TotalCreditsEarned DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReferralCodes_TotalCreditsEarned DEFAULT 0,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_ReferralCodes_Currency DEFAULT 'USD',
        IsActive BIT NOT NULL CONSTRAINT DF_ReferralCodes_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ReferralCodes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_ReferralCodes_UserId ON dbo.ReferralCodes (UserId);
    CREATE INDEX IX_ReferralCodes_TenantId ON dbo.ReferralCodes (TenantId);
END;

IF OBJECT_ID('dbo.ReferralSignups', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReferralSignups
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReferralSignups PRIMARY KEY,
        ReferralCodeId INT NOT NULL CONSTRAINT FK_ReferralSignups_ReferralCodes FOREIGN KEY REFERENCES dbo.ReferralCodes (Id),
        ReferredTenantId INT NULL,
        ReferredUserId INT NULL,
        ReferredEmail NVARCHAR(200) NOT NULL,
        CreditAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_ReferralSignups_CreditAmount DEFAULT 0,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_ReferralSignups_Currency DEFAULT 'USD',
        CreditPaid BIT NOT NULL CONSTRAINT DF_ReferralSignups_CreditPaid DEFAULT 0,
        CreditPaidAt DATETIME2 NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ReferralSignups_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_ReferralSignups_ReferralCodeId ON dbo.ReferralSignups (ReferralCodeId);
END;
""");
    }
}
