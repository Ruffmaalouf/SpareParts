using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds the columns the Intake &amp; Teardown Agent uses to track which used cars still need
/// an automated teardown/parts plan generated, and how many draft parts were created for each.
/// </summary>
public static class UsedCarTeardownMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCars', 'TeardownPlanGeneratedAt') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD TeardownPlanGeneratedAt DATETIME2(0) NULL;
END;

IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCars', 'TeardownPlanPartsCreated') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD TeardownPlanPartsCreated INT NULL;
END;
""");
    }
}
