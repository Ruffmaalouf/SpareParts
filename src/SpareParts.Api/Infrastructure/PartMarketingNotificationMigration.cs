using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds the column the Marketing Agent uses to track which newly-priced, active parts it has
/// already scanned for matching customer demand (part requests / need-board ads), so it never
/// re-scans (and re-notifies for) the same part twice.
/// </summary>
public static class PartMarketingNotificationMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'MarketingNotifiedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Parts ADD MarketingNotifiedAt DATETIME2(0) NULL;
END;
""");
    }
}
