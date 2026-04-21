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
            const string sql = @"DECLARE @TransactionTypeId INT;
                                 SELECT @TransactionTypeId = Id
                                 FROM dbo.TransactionTypes
                                 WHERE TypeKey = @TypeKey;

                                 IF @TransactionTypeId IS NULL
                                 BEGIN
                                     THROW 51000, 'Sale transaction type is not configured.', 1;
                                 END;

                                 INSERT INTO dbo.Transactions
                                 (
                                     TransactionTypeId,
                                     ReferenceId,
                                     TransactionNumber,
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
                                     CreatedByUserId
                                 )
                                 SELECT
                                     @TransactionTypeId,
                                     0,
                                     @InvoiceNumber,
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
                                     tt.CurrencyCode,
                                     tt.CurrencyCode,
                                     @TotalAmount,
                                     @TotalAmount,
                                     @PaidAmount,
                                     @CreatedAt,
                                     @CreatedByUserId
                                 FROM dbo.TransactionTypes tt
                                 WHERE tt.Id = @TransactionTypeId;

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
                    invoice.CreatedAt,
                    invoice.CreatedByUserId
                },
                _session.Transaction);
        }

        public void InsertItems(int invoiceId, IList<SalesInvoiceItem> items)
        {
            var transactionId = ResolveTransactionId(invoiceId);
            var counterCurrencyCode = ResolveCounterCurrencyCode(transactionId);

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
                                     1,
                                     @LineTotal,
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
                    CurrencyCode = counterCurrencyCode,
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
                                        t.TransactionDate AS InvoiceDate,
                                        t.CustomerId,
                                        t.WarehouseId,
                                        t.TotalAmount,
                                        t.PaidAmount
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND (@Query IS NULL OR @Query = N''
                                        OR t.TransactionNumber LIKE N'%' + @Query + N'%'
                                        OR CAST(t.ReferenceId AS NVARCHAR(50)) LIKE N'%' + @Query + N'%')
                                 ORDER BY t.TransactionDate DESC, t.ReferenceId DESC;";

            return _session.Connection.Query<SalesInvoiceLookupDto>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    Query = query?.Trim()
                },
                _session.Transaction).ToList();
        }

        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId)
        {
            const string invoiceSql = @"SELECT
                                               t.ReferenceId AS InvoiceId,
                                               t.TransactionNumber AS InvoiceNumber,
                                               t.TransactionDate AS InvoiceDate,
                                               t.CustomerId,
                                               t.WarehouseId,
                                               t.TotalAmount,
                                               t.PaidAmount
                                        FROM dbo.Transactions t
                                        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                        WHERE tt.TypeKey = @TypeKey
                                          AND t.ReferenceId = @InvoiceId;";

            const string itemsSql = @"SELECT
                                             ti.PartId,
                                             ISNULL(p.Name, N'') AS Description,
                                             CAST(ti.Quantity AS INT) AS Quantity,
                                             ti.UnitPrice
                                      FROM dbo.TransactionItems ti
                                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                      LEFT JOIN dbo.Parts p ON p.Id = ti.PartId
                                      WHERE tt.TypeKey = @TypeKey
                                        AND t.ReferenceId = @InvoiceId
                                      ORDER BY ti.SortOrder, ti.Id;";

            var invoice = _session.Connection.QuerySingleOrDefault<SalesInvoiceDetailsDto>(
                invoiceSql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    InvoiceId = invoiceId
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
                    InvoiceId = invoiceId
                },
                _session.Transaction).ToList();

            return invoice;
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
                                                  TotalCost = @TotalCost,
                                                  TotalBaseAmount = @TotalAmount,
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
                invoice.TotalCost,
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
                                   AND t.ReferenceId = @InvoiceId;";

            return _session.Connection.QuerySingleOrDefault<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Sale,
                    InvoiceId = invoiceId
                },
                _session.Transaction);
        }

        private string ResolveCounterCurrencyCode(int transactionId)
        {
            const string sql = @"SELECT ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD')
                                 FROM dbo.Transactions t
                                 WHERE t.Id = @TransactionId;";

            return _session.Connection.QuerySingle<string>(
                sql,
                new { TransactionId = transactionId },
                _session.Transaction);
        }
    }
}
