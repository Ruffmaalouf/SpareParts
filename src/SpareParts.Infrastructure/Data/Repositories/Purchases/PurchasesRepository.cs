using Dapper;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    public class PurchasesRepository : IPurchasesRepository
    {
        private readonly DbSession _session;

        public PurchasesRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertInvoice(PurchaseInvoice invoice)
        {
            const string sql = @"DECLARE @TransactionTypeId INT;
                                 SELECT @TransactionTypeId = Id
                                 FROM dbo.TransactionTypes
                                 WHERE TypeKey = @TypeKey;

                                 IF @TransactionTypeId IS NULL
                                 BEGIN
                                     THROW 51000, 'Purchase transaction type is not configured.', 1;
                                 END;

                                 INSERT INTO dbo.Transactions
                                 (
                                     TransactionTypeId,
                                     ReferenceId,
                                     TransactionNumber,
                                     TransactionDate,
                                     SupplierId,
                                     WarehouseId,
                                     Subtotal,
                                     DiscountAmount,
                                     TaxAmount,
                                     TotalAmount,
                                     PaidAmount,
                                     PaymentStatus,
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
                                     @PurchaseNumber,
                                     @PurchaseDate,
                                     @SupplierId,
                                     @WarehouseId,
                                     @Subtotal,
                                     @DiscountAmount,
                                     @TaxAmount,
                                     @TotalAmount,
                                     @PaidAmount,
                                     @PaymentStatus,
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
                    TypeKey = TransactionTypeKeys.Purchase,
                    invoice.PurchaseNumber,
                    invoice.PurchaseDate,
                    invoice.SupplierId,
                    invoice.WarehouseId,
                    invoice.Subtotal,
                    invoice.DiscountAmount,
                    invoice.TaxAmount,
                    invoice.TotalAmount,
                    invoice.PaidAmount,
                    invoice.PaymentStatus,
                    invoice.CreatedAt,
                    invoice.CreatedByUserId
                },
                _session.Transaction);
        }

        public bool PurchaseNumberExists(string purchaseNumber)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.TransactionNumber = @PurchaseNumber;";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Purchase,
                    PurchaseNumber = purchaseNumber
                },
                _session.Transaction) > 0;
        }

        public void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items)
        {
            var transactionId = ResolveTransactionId(purchaseId);
            var counterCurrencyCode = ResolveCounterCurrencyCode(transactionId);

            const string sql = @"INSERT INTO dbo.TransactionItems
                                 (
                                     TransactionId,
                                     ItemType,
                                     PartId,
                                     Quantity,
                                     UnitCost,
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
                                     @UnitCost,
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
                item.PurchaseId = purchaseId;

                _session.Connection.Execute(sql, new
                {
                    TransactionId = transactionId,
                    ItemType = "purchase_item",
                    item.PartId,
                    Quantity = (decimal)item.Quantity,
                    item.UnitCost,
                    item.TaxRate,
                    Amount = decimal.Round(item.Quantity * item.UnitCost, 4, MidpointRounding.AwayFromZero),
                    item.LineTotal,
                    CurrencyCode = counterCurrencyCode,
                    SortOrder = index + 1,
                    item.CreatedAt,
                    item.CreatedByUserId
                }, _session.Transaction);
            }
        }

        public List<PurchaseInvoiceLookupDto> SearchPurchases(string? query)
        {
            const string sql = @"SELECT TOP (200)
                                        t.ReferenceId AS PurchaseId,
                                        t.TransactionNumber AS PurchaseNumber,
                                        t.TransactionDate AS PurchaseDate,
                                        t.SupplierId,
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

            return _session.Connection.Query<PurchaseInvoiceLookupDto>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Purchase,
                    Query = query?.Trim()
                },
                _session.Transaction).ToList();
        }

        public PurchaseInvoiceDetailsDto? GetInvoiceById(int purchaseId)
        {
            const string invoiceSql = @"SELECT
                                               t.ReferenceId AS PurchaseId,
                                               t.TransactionNumber AS PurchaseNumber,
                                               t.TransactionDate AS PurchaseDate,
                                               t.SupplierId,
                                               t.WarehouseId,
                                               t.TotalAmount,
                                               t.PaidAmount
                                        FROM dbo.Transactions t
                                        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                        WHERE tt.TypeKey = @TypeKey
                                          AND t.ReferenceId = @PurchaseId;";

            const string itemsSql = @"SELECT
                                             ti.PartId,
                                             ISNULL(p.Name, N'') AS Description,
                                             CAST(ti.Quantity AS INT) AS Quantity,
                                             ti.UnitCost
                                      FROM dbo.TransactionItems ti
                                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                      LEFT JOIN dbo.Parts p ON p.Id = ti.PartId
                                      WHERE tt.TypeKey = @TypeKey
                                        AND t.ReferenceId = @PurchaseId
                                      ORDER BY ti.SortOrder, ti.Id;";

            var invoice = _session.Connection.QuerySingleOrDefault<PurchaseInvoiceDetailsDto>(
                invoiceSql,
                new
                {
                    TypeKey = TransactionTypeKeys.Purchase,
                    PurchaseId = purchaseId
                },
                _session.Transaction);

            if (invoice == null)
            {
                return null;
            }

            invoice.Items = _session.Connection.Query<PurchaseInvoiceLineDto>(
                itemsSql,
                new
                {
                    TypeKey = TransactionTypeKeys.Purchase,
                    PurchaseId = purchaseId
                },
                _session.Transaction).ToList();

            return invoice;
        }

        public bool UpdateInvoice(int purchaseId, PurchaseInvoice invoice, IList<PurchaseInvoiceItem> items, int userId)
        {
            var transactionId = ResolveTransactionIdOrDefault(purchaseId);
            if (transactionId <= 0)
            {
                return false;
            }

            const string updateInvoiceSql = @"UPDATE t
                                              SET TransactionDate = @PurchaseDate,
                                                  SupplierId = @SupplierId,
                                                  WarehouseId = @WarehouseId,
                                                  Subtotal = @Subtotal,
                                                  DiscountAmount = @DiscountAmount,
                                                  TaxAmount = @TaxAmount,
                                                  TotalAmount = @TotalAmount,
                                                  PaidAmount = @PaidAmount,
                                                  PaymentStatus = @PaymentStatus,
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
                invoice.PurchaseDate,
                invoice.SupplierId,
                invoice.WarehouseId,
                invoice.Subtotal,
                invoice.DiscountAmount,
                invoice.TaxAmount,
                invoice.TotalAmount,
                invoice.PaidAmount,
                invoice.PaymentStatus,
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

            InsertItems(purchaseId, items);
            return true;
        }

        private int ResolveTransactionId(int purchaseId)
        {
            var transactionId = ResolveTransactionIdOrDefault(purchaseId);
            if (transactionId <= 0)
            {
                throw new InvalidOperationException($"Purchase transaction {purchaseId} was not found.");
            }

            return transactionId;
        }

        private int ResolveTransactionIdOrDefault(int purchaseId)
        {
            const string sql = @"SELECT t.Id
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @PurchaseId;";

            return _session.Connection.QuerySingleOrDefault<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.Purchase,
                    PurchaseId = purchaseId
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
