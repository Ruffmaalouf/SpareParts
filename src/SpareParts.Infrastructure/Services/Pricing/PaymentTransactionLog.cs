using System.Data;
using Dapper;
using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Shared helper for appending audit rows to dbo.PaymentTransactions.</summary>
internal static class PaymentTransactionLog
{
    public static void Insert(
        IDbConnection connection,
        IDbTransaction? transaction,
        int paymentId,
        string providerCode,
        PaymentTransactionStatus status,
        string? message,
        string? rawPayload = null)
    {
        connection.Execute(
            """
            INSERT INTO dbo.PaymentTransactions (PaymentId, ProviderCode, Status, RawPayload, Message, CreatedAt)
            VALUES (@PaymentId, @ProviderCode, @Status, @RawPayload, @Message, @Now)
            """,
            new
            {
                PaymentId = paymentId,
                ProviderCode = providerCode,
                Status = (int)status,
                RawPayload = rawPayload,
                Message = message,
                Now = DateTime.UtcNow
            },
            transaction);
    }
}
