using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarStateEventsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.UsedCarStateEvents', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCarStateEvents
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCarStateEvents PRIMARY KEY,
        UsedCarId INT NOT NULL,
        EventType NVARCHAR(40) NOT NULL,
        Mileage DECIMAL(18, 2) NULL,
        Condition NVARCHAR(40) NULL,
        Location NVARCHAR(160) NULL,
        Note NVARCHAR(1000) NULL,
        RecordedAt DATETIME2(0) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCarStateEvents_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_UsedCarStateEvents_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars (Id),
        CONSTRAINT FK_UsedCarStateEvents_Users FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_UsedCarStateEvents_UsedCarId ON dbo.UsedCarStateEvents (UsedCarId);
END;");
    }
}
