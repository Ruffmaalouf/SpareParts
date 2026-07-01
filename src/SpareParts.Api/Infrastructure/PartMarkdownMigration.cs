using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds the column the Dead Stock Markdown Agent uses to record when it last discounted a part.
/// </summary>
public static class PartMarkdownMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'LastMarkdownAppliedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD LastMarkdownAppliedAt DATETIME2(0) NULL;
END;
""");
    }
}
