using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartSubstitutesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.PartSubstitutes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartSubstitutes
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartSubstitutes PRIMARY KEY,
        PartId INT NOT NULL,
        SubstitutePartId INT NOT NULL,
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PartSubstitutes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_PartSubstitutes_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT FK_PartSubstitutes_SubstituteParts FOREIGN KEY (SubstitutePartId) REFERENCES dbo.Parts (Id),
        CONSTRAINT UQ_PartSubstitutes_Pair UNIQUE (PartId, SubstitutePartId),
        CONSTRAINT CK_PartSubstitutes_NoSelfRef CHECK (PartId <> SubstitutePartId)
    );
END;
""");
    }
}
