using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class ReorderRulesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.ReorderRules', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReorderRules
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReorderRules PRIMARY KEY,
        PartId INT NOT NULL,
        ReorderPoint INT NOT NULL CONSTRAINT DF_ReorderRules_ReorderPoint DEFAULT 0,
        ReorderQuantity INT NOT NULL CONSTRAINT DF_ReorderRules_ReorderQuantity DEFAULT 1,
        PreferredSupplierId INT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ReorderRules_IsActive DEFAULT 1,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ReorderRules_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_ReorderRules_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT UQ_ReorderRules_PartId UNIQUE (PartId)
    );
END;
""");
    }
}
