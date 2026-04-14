using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class CarModelsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.CarModels', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.CarModels', 'BodyType') IS NULL
BEGIN
    ALTER TABLE dbo.CarModels ADD BodyType NVARCHAR(60) NULL;
END;");
    }
}
