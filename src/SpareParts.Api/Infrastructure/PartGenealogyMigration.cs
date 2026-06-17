using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartGenealogyMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.PartGenealogyEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartGenealogyEvents
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartGenealogyEvents PRIMARY KEY,
        PartId             INT NOT NULL,
        EventType          TINYINT NOT NULL,
        Description        NVARCHAR(500) NULL,
        ActorName          NVARCHAR(200) NULL,
        TransactionRef     NVARCHAR(100) NULL,
        OccurredAt         DATETIME2 NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_PartGenealogyEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL
    );
    CREATE INDEX IX_PartGenealogyEvents_PartId ON dbo.PartGenealogyEvents (PartId);
END;
""");
    }
}
