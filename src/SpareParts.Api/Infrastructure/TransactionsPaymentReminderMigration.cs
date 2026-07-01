using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

/// <summary>
/// Adds the column the AR Collections Agent uses to avoid re-sending a payment reminder for the
/// same invoice every single day.
/// </summary>
public static class TransactionsPaymentReminderMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Transactions', 'LastPaymentReminderSentAt') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD LastPaymentReminderSentAt DATETIME2(0) NULL;
END;
""");
    }
}
