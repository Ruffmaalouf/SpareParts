using Dapper;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class UtcInvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        private readonly ISqlConnectionFactory _factory;

        public UtcInvoiceNumberGenerator(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public string NextSalesNumber() => NextTransactionNumber(TransactionTypeKeys.Sale);

        public string NextSalesReturnNumber() => NextTransactionNumber(TransactionTypeKeys.SalesReturn);

        public string NextPurchaseNumber() => NextTransactionNumber(TransactionTypeKeys.Purchase);

        public string NextUsedCarPurchaseNumber() => NextTransactionNumber(TransactionTypeKeys.UsedCarPurchase);

        private string NextTransactionNumber(string typeKey)
        {
            using var connection = _factory.CreateConnection();
            var nowUtc = DateTime.UtcNow;
            for (var attempt = 0; attempt < 100000; attempt++)
            {
                var nextState = connection.QuerySingleOrDefault<TransactionTypeNumberState>(
                    @"UPDATE dbo.TransactionTypes
                      SET SerialCurrentNumber = CASE
                          WHEN ISNULL(SerialCurrentNumber, 0) + 1 < ISNULL(SerialStartNumber, 1)
                              THEN ISNULL(SerialStartNumber, 1)
                          ELSE ISNULL(SerialCurrentNumber, 0) + 1
                      END
                      OUTPUT INSERTED.TypeKey,
                             INSERTED.Name,
                             INSERTED.SerialNumberFormat,
                             INSERTED.SerialCurrentNumber
                      WHERE TypeKey = @TypeKey;",
                    new { TypeKey = typeKey });

                if (nextState == null)
                {
                    throw new InvalidOperationException($"Transaction type '{typeKey}' is not configured.");
                }

                var nextNumber = TransactionSerialNumberFormatter.Format(
                    nextState.SerialNumberFormat,
                    nowUtc,
                    nextState.SerialCurrentNumber,
                    nextState.TypeKey,
                    nextState.Name);

                var exists = connection.ExecuteScalar<int>(
                    @"SELECT COUNT(1)
                      FROM dbo.Transactions t
                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                      WHERE tt.TypeKey = @TypeKey
                        AND t.TransactionNumber = @TransactionNumber;",
                    new
                    {
                        TypeKey = typeKey,
                        TransactionNumber = nextNumber
                    }) > 0;

                if (!exists)
                {
                    return nextNumber;
                }
            }

            throw new InvalidOperationException($"Could not allocate a unique transaction number for type '{typeKey}'.");
        }
    }
}
