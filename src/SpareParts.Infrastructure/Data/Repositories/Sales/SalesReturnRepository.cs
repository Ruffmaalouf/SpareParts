using Dapper;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    public class SalesReturnRepository : ISalesReturnRepository
    {
        private readonly DbSession _session;

        public SalesReturnRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertReturn(SalesReturn salesReturn)
        {
            var currencyContext = AccountingCurrencyContextResolver.Resolve(_session);
            const string sql = @"DECLARE @TransactionTypeId INT;
                                 SELECT @TransactionTypeId = Id
                                 FROM dbo.TransactionTypes
                                 WHERE TypeKey = @TypeKey;

                                 IF @TransactionTypeId IS NULL
                                 BEGIN
                                     THROW 51000, 'Sale return transaction type is not configured.', 1;
                                 END;

                                 DECLARE @TotalBaseAmount DECIMAL(19, 4) = CASE
                                     WHEN @BaseCurrencyCode = @CounterCurrencyCode THEN @TotalAmount
                                     ELSE ROUND(@TotalAmount * @CounterRateToBase, 4)
                                 END;

                                 INSERT INTO dbo.Transactions
                                 (
                                     TransactionTypeId,
                                     ReferenceId,
                                     TransactionNumber,
                                     ScanCode,
                                     TransactionDate,
                                     CustomerId,
                                     WarehouseId,
                                     Subtotal,
                                     DiscountAmount,
                                     TaxAmount,
                                     TotalAmount,
                                     PaidAmount,
                                     PaymentStatus,
                                     Notes,
                                     IsReturn,
                                     ParentReferenceId,
                                     TotalCost,
                                     BaseCurrencyCode,
                                     CounterCurrencyCode,
                                     TotalBaseAmount,
                                     TotalCounterAmount,
                                     PaidCounterAmount,
                                     CreatedAt,
                                     CreatedByUserId
                                 )
                                 SELECT
                                     @TransactionTypeId,
                                     0,
                                     @ReturnNumber,
                                     @ReturnNumber,
                                     @ReturnDate,
                                     @CustomerId,
                                     @WarehouseId,
                                     @TotalAmount,
                                     0,
                                     0,
                                     @TotalAmount,
                                     @RefundAmount,
                                     CASE WHEN @RefundAmount >= @TotalAmount THEN 'Paid' ELSE 'Unpaid' END,
                                     @Reason,
                                     1,
                                     @OriginalInvoiceId,
                                     @TotalCost,
                                     @BaseCurrencyCode,
                                     @CounterCurrencyCode,
                                     @TotalBaseAmount,
                                     @TotalAmount,
                                     @RefundAmount,
                                     @CreatedAt,
                                     @CreatedByUserId
                                 ;

                                 DECLARE @ReturnId INT = CAST(SCOPE_IDENTITY() AS INT);
                                 UPDATE dbo.Transactions
                                 SET ReferenceId = @ReturnId
                                 WHERE Id = @ReturnId;

                                 SELECT @ReturnId;";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.SalesReturn,
                    salesReturn.ReturnNumber,
                    salesReturn.ReturnDate,
                    salesReturn.CustomerId,
                    salesReturn.WarehouseId,
                    salesReturn.TotalAmount,
                    salesReturn.RefundAmount,
                    salesReturn.OriginalInvoiceId,
                    salesReturn.TotalCost,
                    salesReturn.Reason,
                    currencyContext.BaseCurrencyCode,
                    currencyContext.CounterCurrencyCode,
                    currencyContext.CounterRateToBase,
                    salesReturn.CreatedAt,
                    salesReturn.CreatedByUserId
                },
                _session.Transaction);
        }

        public void InsertItems(int returnId, IList<SalesInvoiceItem> items)
        {
            const string resolveIdSql = @"SELECT t.Id
                                          FROM dbo.Transactions t
                                          INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                          WHERE tt.TypeKey = @TypeKey
                                            AND t.ReferenceId = @ReturnId;";

            var transactionId = _session.Connection.QuerySingleOrDefault<int>(
                resolveIdSql,
                new { TypeKey = TransactionTypeKeys.SalesReturn, ReturnId = returnId },
                _session.Transaction);

            if (transactionId <= 0)
            {
                throw new InvalidOperationException($"Sales return transaction {returnId} was not found.");
            }

            const string currencySql = @"SELECT
                                             CounterCurrencyCode = ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD'),
                                             CounterRateToBase = CASE
                                                 WHEN ISNULL(t.TotalCounterAmount, 0) > 0 AND ISNULL(t.TotalBaseAmount, 0) > 0
                                                 THEN ROUND(t.TotalBaseAmount / t.TotalCounterAmount, 8)
                                                 ELSE 1
                                             END
                                         FROM dbo.Transactions t
                                         WHERE t.Id = @TransactionId;";

            var currencyRow = _session.Connection.QuerySingle<ReturnCurrencyRow>(
                currencySql, new { TransactionId = transactionId }, _session.Transaction);

            const string sql = @"INSERT INTO dbo.TransactionItems
                                 (
                                     TransactionId, ItemType, PartId, Quantity,
                                     UnitPrice, DiscountAmount, TaxRate, Amount, LineTotal,
                                     CurrencyCode, RateToBase, BaseAmount, CounterAmount,
                                     SortOrder, CreatedAt, CreatedByUserId
                                 )
                                 VALUES
                                 (
                                     @TransactionId, @ItemType, @PartId, @Quantity,
                                     @UnitPrice, 0, @TaxRate, @Amount, @LineTotal,
                                     @CurrencyCode, @RateToBase, @BaseAmount, @LineTotal,
                                     @SortOrder, @CreatedAt, @CreatedByUserId
                                 );";

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                item.InvoiceId = returnId;

                _session.Connection.Execute(sql, new
                {
                    TransactionId = transactionId,
                    ItemType = "return_item",
                    item.PartId,
                    Quantity = (decimal)item.Quantity,
                    item.UnitPrice,
                    item.TaxRate,
                    Amount = decimal.Round(item.Quantity * item.UnitPrice, 4, MidpointRounding.AwayFromZero),
                    item.LineTotal,
                    CurrencyCode = currencyRow.CounterCurrencyCode,
                    RateToBase = currencyRow.CounterRateToBase,
                    BaseAmount = decimal.Round(item.LineTotal * currencyRow.CounterRateToBase, 4, MidpointRounding.AwayFromZero),
                    SortOrder = index + 1,
                    item.CreatedAt,
                    item.CreatedByUserId
                }, _session.Transaction);
            }
        }

        public bool ReturnNumberExists(string returnNumber)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.TransactionNumber = @ReturnNumber;";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new { TypeKey = TransactionTypeKeys.SalesReturn, ReturnNumber = returnNumber },
                _session.Transaction) > 0;
        }

        public List<SalesReturnLookupDto> SearchReturns(string? query)
        {
            const string sql = @"SELECT TOP (200)
                                        t.ReferenceId AS ReturnId,
                                        t.TransactionNumber AS ReturnNumber,
                                        t.ParentReferenceId AS OriginalInvoiceId,
                                        ISNULL(orig.TransactionNumber, N'') AS OriginalInvoiceNumber,
                                        t.TransactionDate AS ReturnDate,
                                        t.CustomerId,
                                        t.TotalAmount,
                                        ISNULL(t.PaidAmount, 0) AS RefundAmount
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 LEFT JOIN dbo.Transactions orig ON orig.ReferenceId = t.ParentReferenceId
                                     AND orig.TransactionTypeId IN (
                                         SELECT Id FROM dbo.TransactionTypes WHERE TypeKey = @SaleTypeKey
                                     )
                                 WHERE tt.TypeKey = @TypeKey
                                   AND (@Query IS NULL OR @Query = N''
                                        OR t.TransactionNumber LIKE N'%' + @Query + N'%'
                                        OR orig.TransactionNumber LIKE N'%' + @Query + N'%'
                                        OR CAST(t.ReferenceId AS NVARCHAR(50)) LIKE N'%' + @Query + N'%')
                                 ORDER BY t.TransactionDate DESC, t.ReferenceId DESC;";

            return _session.Connection.Query<SalesReturnLookupDto>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.SalesReturn,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    Query = query?.Trim()
                },
                _session.Transaction).ToList();
        }

        public SalesReturnDetailsDto? GetReturnById(int returnId)
        {
            const string invoiceSql = @"SELECT
                                               t.ReferenceId AS ReturnId,
                                               t.TransactionNumber AS ReturnNumber,
                                               t.ParentReferenceId AS OriginalInvoiceId,
                                               ISNULL(orig.TransactionNumber, N'') AS OriginalInvoiceNumber,
                                               t.TransactionDate AS ReturnDate,
                                               t.CustomerId,
                                               t.WarehouseId,
                                               t.TotalAmount,
                                               ISNULL(t.PaidAmount, 0) AS RefundAmount,
                                               t.Notes AS Reason
                                        FROM dbo.Transactions t
                                        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                        LEFT JOIN dbo.Transactions orig ON orig.ReferenceId = t.ParentReferenceId
                                            AND orig.TransactionTypeId IN (
                                                SELECT Id FROM dbo.TransactionTypes WHERE TypeKey = @SaleTypeKey
                                            )
                                        WHERE tt.TypeKey = @TypeKey
                                          AND t.ReferenceId = @ReturnId;";

            const string itemsSql = @"SELECT
                                             ti.PartId,
                                             ISNULL(p.Name, N'') AS Description,
                                             CAST(ti.Quantity AS INT) AS Quantity,
                                             ti.UnitPrice,
                                             ISNULL(ti.TaxRate, 0) AS TaxRate,
                                             ti.LineTotal
                                      FROM dbo.TransactionItems ti
                                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                      LEFT JOIN dbo.Parts p ON p.Id = ti.PartId
                                      WHERE tt.TypeKey = @TypeKey
                                        AND t.ReferenceId = @ReturnId
                                      ORDER BY ti.SortOrder, ti.Id;";

            var dto = _session.Connection.QuerySingleOrDefault<SalesReturnDetailsDto>(
                invoiceSql,
                new
                {
                    TypeKey = TransactionTypeKeys.SalesReturn,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    ReturnId = returnId
                },
                _session.Transaction);

            if (dto == null)
            {
                return null;
            }

            dto.Items = _session.Connection.Query<SalesReturnLineDto>(
                itemsSql,
                new { TypeKey = TransactionTypeKeys.SalesReturn, ReturnId = returnId },
                _session.Transaction).ToList();

            return dto;
        }

        public List<ReturnableLineDto> GetReturnableLines(int originalInvoiceId)
        {
            const string sql = @"SELECT
                                        ti.PartId,
                                        ISNULL(p.Name, N'') AS Description,
                                        CAST(ti.Quantity AS INT) AS OriginalQuantity,
                                        ISNULL(SUM(CAST(ret_ti.Quantity AS INT)), 0) AS AlreadyReturnedQuantity,
                                        CAST(ti.Quantity AS INT) - ISNULL(SUM(CAST(ret_ti.Quantity AS INT)), 0) AS ReturnableQuantity,
                                        ti.UnitPrice,
                                        ISNULL(ti.TaxRate, 0) AS TaxRate
                                 FROM dbo.TransactionItems ti
                                 INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 LEFT JOIN dbo.Parts p ON p.Id = ti.PartId
                                 LEFT JOIN dbo.Transactions ret ON ret.ParentReferenceId = t.ReferenceId
                                     AND ret.TransactionTypeId IN (
                                         SELECT Id FROM dbo.TransactionTypes WHERE TypeKey = @ReturnTypeKey
                                     )
                                 LEFT JOIN dbo.TransactionItems ret_ti ON ret_ti.TransactionId = ret.Id
                                     AND ret_ti.PartId = ti.PartId
                                 WHERE tt.TypeKey = @SaleTypeKey
                                   AND t.ReferenceId = @OriginalInvoiceId
                                 GROUP BY ti.PartId, p.Name, ti.Quantity, ti.UnitPrice, ti.TaxRate
                                 HAVING CAST(ti.Quantity AS INT) - ISNULL(SUM(CAST(ret_ti.Quantity AS INT)), 0) > 0
                                 ORDER BY ti.PartId;";

            return _session.Connection.Query<ReturnableLineDto>(
                sql,
                new
                {
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    ReturnTypeKey = TransactionTypeKeys.SalesReturn,
                    OriginalInvoiceId = originalInvoiceId
                },
                _session.Transaction).ToList();
        }

        private sealed class ReturnCurrencyRow
        {
            public string CounterCurrencyCode { get; set; } = "USD";
            public decimal CounterRateToBase { get; set; } = 1m;
        }
    }
}
