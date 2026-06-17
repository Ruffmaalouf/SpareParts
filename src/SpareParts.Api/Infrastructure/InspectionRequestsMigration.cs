using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class InspectionRequestsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.InspectionRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InspectionRequests
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InspectionRequests PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_InspectionRequests_TenantId DEFAULT 0,
        PartId             INT NOT NULL,
        BuyerName          NVARCHAR(200) NOT NULL,
        BuyerPhone         NVARCHAR(50) NULL,
        RequestedAt        DATETIME2 NULL,
        Notes              NVARCHAR(500) NULL,
        Status             TINYINT NOT NULL CONSTRAINT DF_InspectionRequests_Status DEFAULT 1,
        VideoProvider      NVARCHAR(50) NULL,
        VideoRoomUrl       NVARCHAR(500) NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_InspectionRequests_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL,
        ModifiedAt         DATETIME2 NULL,
        ModifiedByUserId   INT NULL
    );
    CREATE INDEX IX_InspectionRequests_PartId ON dbo.InspectionRequests (PartId);
    CREATE INDEX IX_InspectionRequests_TenantId ON dbo.InspectionRequests (TenantId);
END;
""");
    }
}
