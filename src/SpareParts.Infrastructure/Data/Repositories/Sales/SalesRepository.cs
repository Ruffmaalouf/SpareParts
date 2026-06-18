using Dapper;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    public class SalesRepository : ISalesRepository
    {
        private readonly DbSession _session;

        public SalesRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertInvoice(SalesInvoice invoice)
        {
            var currencyContext = AccountingCurrencyContextResolver.Resolve(_session);
            const string sql = @"DECLARE @TransactionTypeId INT;
                                 SELECT @TransactionTypeId = Id
                                 FROM dbo.TransactionTypes
                                 WHERE TypeKey = @TypeKey;

                                 IF @TransactionTypeId IS NULL
                                 BEGIN
                                     THROW 51000, 'Sale transaction type is not configured.', 1;
                                 END;

                                 DECLARE @TotalBaseAmount DECIMAL(19, 4) = CASE
                                     WHEN @BaseCurrencyCode = @CounterCurrencyCode THEN @TotalAmount
                                     ELSE ROUND(@TotalAmount * @CounterRateToBase, 4)
                                 END;

                                 DECLARE @PaidBaseAmount DECIMAL(19, 4) = CASE
                                     WHEN @BaseCurrencyCode = @CounterCurrencyCode THEN @PaidAmount
                                     ELSE ROUND(@PaidAmount * @CounterRateToBase, 4)
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
                                     PaymentMethod,
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
                                     CreatedByUserId,
                                     TenantId
                                 )
                                 SELECT
                                     @TransactionTypeId,
                                     0,
                                     @InvoiceNumber,
                                     @ScanCode,
                                     @InvoiceDate,
                                     @CustomerId,
                                     @WarehouseId,
                                     @Subtotal,
                                     @DiscountAmount,
                                     @TaxAmount,
                                     @TotalAmount,
                                     @PaidAmount,
                                     @PaymentStatus,
                                     @PaymentMethod,
                                     @Notes,
                                     @IsReturn,
                                     @ParentInvoiceId,
                                     @TotalCost,
                                     @BaseCurrencyCode,
                                     @CounterCurrencyCode,
                                     @TotalBaseAmount,
                                     @TotalAmount,
                                     @PaidAmount,
                                     @CreatedAt,
                                     @CreatedByUserId,
                                     @TenantId
                                 ;

                                 DECLARE @TransactionId INT = CAST(SCOPE_IDENTITY() AS INT);
                                 UPDATE dbo.Transactions
                                 SET ReferenceId = @TransactionId
                                 WHERE Id = @TransactionId;

                                 SELECT @TransactionId;";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    invoice.InvoiceNumber,
                    ScanCode = string.IsNullOrWhiteSpace(invoice.ScanCode) ? invoice.InvoiceNumber : invoice.ScanCode.Trim(),
                    invoice.InvoiceDate,
                    invoice.CustomerId,
                    invoice.WarehouseId,
                    invoice.Subtotal,
                    invoice.DiscountAmount,
                    invoice.TaxAmount,
                    invoice.TotalAmount,
                    invoice.PaidAmount,
                    invoice.PaymentStatus,
                    invoice.PaymentMethod,
                    invoice.Notes,
                    invoice.IsReturn,
                    ParentInvoiceId = invoice.ParentInvoiceId,
                    invoice.TotalCost,
                    currencyContext.BaseCurrencyCode,
                    currencyContext.CounterCurrencyCode,
                    currencyContext.CounterRateToBase,
                    invoice.CreatedAt,
                    invoice.CreatedByUserId,
                    TenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null
                },
                _session.Transaction);
        }

        public void InsertItems(int invoiceId, IList<SalesInvoiceItem> items)
        {
            var transactionId = ResolveTransactionId(invoiceId);
            var currencyContext = ResolveTransactionCurrencyContext(transactionId);

            const string sql = @"INSERT INTO dbo.TransactionItems
                                 (
                                     TransactionId,
                                     ItemType,
                                     PartId,
                                     Quantity,
                                     UnitPrice,
                                     DiscountAmount,
                                     TaxRate,
                                     Amount,
                                     LineTotal,
                                     CurrencyCode,
                                     RateToBase,
                                     BaseAmount,
                                     CounterAmount,
                                     SortOrder,
                                     CreatedAt,
                                     CreatedByUserId
                                 )
                                 VALUES
                                 (
                                     @TransactionId,
                                     @ItemType,
                                     @PartId,
                                     @Quantity,
                                     @UnitPrice,
                                     @DiscountAmount,
                                     @TaxRate,
                                     @Amount,
                                     @LineTotal,
                                     @CurrencyCode,
                                     @RateToBase,
                                     @BaseAmount,
                                     @LineTotal,
                                     @SortOrder,
                                     @CreatedAt,
                                     @CreatedByUserId
                                 );";

            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                item.InvoiceId = invoiceId;

                _session.Connection.Execute(sql, new
                {
                    TransactionId = transactionId,
                    ItemType = "sale_item",
                    item.PartId,
                    Quantity = (decimal)item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    item.TaxRate,
                    Amount = decimal.Round(item.Quantity * item.UnitPrice, 4, MidpointRounding.AwayFromZero),
                    item.LineTotal,
                    CurrencyCode = currencyContext.CounterCurrencyCode,
                    RateToBase = currencyContext.CounterRateToBase,
                    BaseAmount = decimal.Round(item.LineTotal * currencyContext.CounterRateToBase, 4, MidpointRounding.AwayFromZero),
                    SortOrder = index + 1,
                    item.CreatedAt,
                    item.CreatedByUserId
                }, _session.Transaction);
            }
        }

        public List<SalesInvoiceLookupDto> SearchInvoices(string? query)
        {
            const string sql = @"SELECT TOP (200)
                                        t.ReferenceId AS InvoiceId,
                                        t.TransactionNumber AS InvoiceNumber,
                                        t.ScanCode,
                                        t.TransactionDate AS InvoiceDate,
                                        t.CustomerId,
                                        c.Name AS CustomerName,
                                        t.WarehouseId,
                                        t.TotalAmount,
                                        t.PaidAmount,
                                        ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD') AS CounterCurrencyCode
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 LEFT JOIN dbo.Customers c ON c.Id = t.CustomerId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND (@TenantId = 0 OR t.TenantId = @TenantId)
                                   AND (@Query IS NULL OR @Query = N''
                                        OR t.TransactionNumber LIKE N'%' + @Query + N'%'
                                        OR t.ScanCode LIKE N'%' + @Query + N'%'
                                        OR CAST(t.ReferenceId AS NVARCHAR(50)) LIKE N'%' + @Query + N'%')
                                 ORDER BY t.TransactionDate DESC, t.ReferenceId DESC;";

            return _session.Connection.Query<SalesInvoiceLookupDto>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    Query = query?.Trim(),
                    TenantId = _session.TenantId
                },
                _session.Transaction).ToList();
        }

        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId)
        {
            const string invoiceSql = @"SELECT
                                               t.ReferenceId AS InvoiceId,
                                               t.TransactionNumber AS InvoiceNumber,
                                               t.ScanCode,
                                               t.TransactionDate AS InvoiceDate,
                                               t.CustomerId,
                                               t.WarehouseId,
                                               t.TotalAmount,
                                               t.PaidAmount,
                                               ISNULL(NULLIF(t.BaseCurrencyCode, N''), N'USD') AS BaseCurrencyCode,
                                               ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD') AS CounterCurrencyCode,
                                               CASE
                                                   WHEN ISNULL(t.TotalCounterAmount, 0) > 0 AND ISNULL(t.TotalBaseAmount, 0) > 0
                                                   THEN ROUND(t.TotalBaseAmount / t.TotalCounterAmount, 8)
                                                   ELSE 1
                                               END AS CounterRateToBase
                                        FROM dbo.Transactions t
                                        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                        WHERE tt.TypeKey = @TypeKey
                                          AND t.ReferenceId = @InvoiceId
                                          AND (@TenantId = 0 OR t.TenantId = @TenantId);";

            const string itemsSql = @"SELECT
                                             ti.PartId,
                                             ISNULL(p.Name, N'') AS Description,
                                             CAST(ti.Quantity AS INT) AS Quantity,
                                             ti.UnitPrice,
                                             ISNULL(p.CostPrice, 0) AS CostPrice
                                      FROM dbo.TransactionItems ti
                                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                      LEFT JOIN dbo.Parts p ON p.Id = ti.PartId
                                      WHERE tt.TypeKey = @TypeKey
                                        AND t.ReferenceId = @InvoiceId
                                        AND (@TenantId = 0 OR t.TenantId = @TenantId)
                                      ORDER BY ti.SortOrder, ti.Id;";

            var invoice = _session.Connection.QuerySingleOrDefault<SalesInvoiceDetailsDto>(
                invoiceSql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    InvoiceId = invoiceId,
                    TenantId = _session.TenantId
                },
                _session.Transaction);

            if (invoice == null)
            {
                return null;
            }

            invoice.Items = _session.Connection.Query<SalesInvoiceLineDto>(
                itemsSql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    InvoiceId = invoiceId,
                    TenantId = _session.TenantId
                },
                _session.Transaction).ToList();
            invoice.Timeline = new TransactionTimelineReader(_session).Build(TransactionTypeKeys.Sale, invoiceId);

            return invoice;
        }

        public List<SalesProfitHistoryPointDto> GetProfitHistory(int months)
        {
            if (months <= 0)
            {
                months = 1;
            }

            var now = DateTime.UtcNow;
            var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(months - 1));

            const string sql = @"SELECT
                                        t.Id AS TransactionId,
                                        t.TransactionDate,
                                        ti.Quantity,
                                        ti.UnitPrice,
                                        ISNULL(p.CostPrice, 0) AS CostPrice
                                  FROM dbo.TransactionItems ti
                                  INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                  LEFT JOIN dbo.Parts p ON p.Id = ti.PartId
                                  WHERE tt.TypeKey = @TypeKey
                                    AND t.TransactionDate >= @StartDate
                                    AND (@TenantId = 0 OR t.TenantId = @TenantId);";

            var lines = _session.Connection.Query<SalesProfitHistoryRawLine>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    StartDate = startDate,
                    TenantId = _session.TenantId
                },
                _session.Transaction);

            return SalesProfitHistoryBuilder.Build(lines, months, now);
        }

        public bool InvoiceNumberExists(string invoiceNumber)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.TransactionNumber = @InvoiceNumber;";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    InvoiceNumber = invoiceNumber
                },
                _session.Transaction) > 0;
        }

        public bool UpdateInvoice(int invoiceId, SalesInvoice invoice, IList<SalesInvoiceItem> items, int userId)
        {
            var transactionId = ResolveTransactionIdOrDefault(invoiceId);
            if (transactionId <= 0)
            {
                return false;
            }

            var currencyContext = AccountingCurrencyContextResolver.Resolve(_session);
            const string updateInvoiceSql = @"UPDATE t
                                              SET TransactionDate = @InvoiceDate,
                                                  CustomerId = @CustomerId,
                                                  WarehouseId = @WarehouseId,
                                                  Subtotal = @Subtotal,
                                                  DiscountAmount = @DiscountAmount,
                                                  TaxAmount = @TaxAmount,
                                                  TotalAmount = @TotalAmount,
                                                  PaidAmount = @PaidAmount,
                                                  PaymentStatus = @PaymentStatus,
                                                  ScanCode = COALESCE(NULLIF(@ScanCode, N''), NULLIF(t.ScanCode, N''), t.TransactionNumber),
                                                  TotalCost = @TotalCost,
                                                  BaseCurrencyCode = @BaseCurrencyCode,
                                                  CounterCurrencyCode = @CounterCurrencyCode,
                                                  TotalBaseAmount = CASE
                                                      WHEN @BaseCurrencyCode = @CounterCurrencyCode THEN @TotalAmount
                                                      ELSE ROUND(@TotalAmount * @CounterRateToBase, 4)
                                                  END,
                                                  TotalCounterAmount = @TotalAmount,
                                                  PaidCounterAmount = @PaidAmount,
                                                  ModifiedAt = @ModifiedAt,
                                                  ModifiedByUserId = @ModifiedByUserId
                                              FROM dbo.Transactions t
                                              WHERE t.Id = @TransactionId;";

            var updated = _session.Connection.Execute(updateInvoiceSql, new
            {
                TransactionId = transactionId,
                invoice.InvoiceDate,
                invoice.CustomerId,
                invoice.WarehouseId,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                invoice.PaidAmount,
                invoice.PaymentStatus,
                ScanCode = string.IsNullOrWhiteSpace(invoice.ScanCode) ? invoice.InvoiceNumber : invoice.ScanCode.Trim(),
                invoice.TotalCost,
                currencyContext.BaseCurrencyCode,
                currencyContext.CounterCurrencyCode,
                currencyContext.CounterRateToBase,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId
            }, _session.Transaction);

            if (updated == 0)
            {
                return false;
            }

            const string deleteItemsSql = @"DELETE FROM dbo.TransactionItems
                                            WHERE TransactionId = @TransactionId;";

            _session.Connection.Execute(deleteItemsSql, new { TransactionId = transactionId }, _session.Transaction);
            InsertItems(invoiceId, items);
            return true;
        }

        public bool ApplyPayment(int invoiceId, decimal paidAmount, string paymentStatus, string? paymentMethod, int userId)
        {
            var transactionId = ResolveTransactionIdOrDefault(invoiceId);
            if (transactionId <= 0)
            {
                return false;
            }

            const string sql = @"UPDATE dbo.Transactions
                                 SET PaidAmount = @PaidAmount,
                                     PaidCounterAmount = @PaidAmount,
                                     PaymentStatus = @PaymentStatus,
                                     PaymentMethod = COALESCE(NULLIF(@PaymentMethod, N''), PaymentMethod),
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 WHERE Id = @TransactionId;";

            return _session.Connection.Execute(sql, new
            {
                TransactionId = transactionId,
                PaidAmount = paidAmount,
                PaymentStatus = paymentStatus,
                PaymentMethod = paymentMethod?.Trim(),
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId
            }, _session.Transaction) > 0;
        }

        private int ResolveTransactionId(int invoiceId)
        {
            var transactionId = ResolveTransactionIdOrDefault(invoiceId);
            if (transactionId <= 0)
            {
                throw new InvalidOperationException($"Sale transaction {invoiceId} was not found.");
            }

            return transactionId;
        }

        private int ResolveTransactionIdOrDefault(int invoiceId)
        {
            const string sql = @"SELECT t.Id
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @InvoiceId
                                   AND (@TenantId = 0 OR t.TenantId = @TenantId);";

            return _session.Connection.QuerySingleOrDefault<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    InvoiceId = invoiceId,
                    TenantId = _session.TenantId
                },
                _session.Transaction);
        }

        private SalesTransactionCurrencyContext ResolveTransactionCurrencyContext(int transactionId)
        {
            const string sql = @"SELECT
                                     CounterCurrencyCode = ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD'),
                                     CounterRateToBase = CASE
                                         WHEN ISNULL(t.TotalCounterAmount, 0) > 0 AND ISNULL(t.TotalBaseAmount, 0) > 0
                                         THEN ROUND(t.TotalBaseAmount / t.TotalCounterAmount, 8)
                                         ELSE 1
                                     END
                                 FROM dbo.Transactions t
                                 WHERE t.Id = @TransactionId;";

            var context = _session.Connection.QuerySingle<SalesTransactionCurrencyContext>(
                sql,
                new { TransactionId = transactionId },
                _session.Transaction);

            if (context.CounterRateToBase <= 0m)
            {
                context.CounterRateToBase = 1m;
            }

            return context;
        }

    }
}
