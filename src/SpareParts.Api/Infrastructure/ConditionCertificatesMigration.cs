using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ConditionCertificatesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.ConditionCertificates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConditionCertificates
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConditionCertificates PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_ConditionCertificates_TenantId DEFAULT 0,
        PartId             INT NOT NULL,
        Grade              TINYINT NOT NULL,
        GradeLabel         NVARCHAR(10) NOT NULL,
        PhotoUrls          NVARCHAR(2000) NULL,
        Notes              NVARCHAR(500) NULL,
        CertificateNumber  NVARCHAR(50) NOT NULL,
        ScanMethod         NVARCHAR(50) NOT NULL CONSTRAINT DF_ConditionCertificates_ScanMethod DEFAULT 'Mock',
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_ConditionCertificates_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL
    );
    CREATE INDEX IX_ConditionCertificates_PartId ON dbo.ConditionCertificates (PartId);
END;
""");
    }
}
