using Dapper;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    public sealed class UsedCarPurchasesRepository : IUsedCarPurchasesRepository
    {
        private readonly DbSession _session;

        public UsedCarPurchasesRepository(DbSession session)
        {
            _session = session;
        }

        public int Insert(UsedCarPurchase purchase)
        {
            const string sql = @"DECLARE @TransactionTypeId INT;
                                 SELECT @TransactionTypeId = Id
                                 FROM dbo.TransactionTypes
                                 WHERE TypeKey = @TypeKey;

                                 IF @TransactionTypeId IS NULL
                                 BEGIN
                                     THROW 51000, 'Used-car purchase transaction type is not configured.', 1;
                                 END;

                                 INSERT INTO dbo.Transactions
                                 (
                                     TransactionTypeId,
                                     ReferenceId,
                                     TransactionNumber,
                                     ScanCode,
                                     TransactionDate,
                                     SupplierId,
                                     UsedCarId,
                                     TotalAmount,
                                     PaidAmount,
                                     PaymentStatus,
                                     Notes,
                                     PostingStatus,
                                     PostedAt,
                                     PostedByUserId,
                                     BaseCurrencyCode,
                                     CounterCurrencyCode,
                                     TotalBaseAmount,
                                     TotalCounterAmount,
                                     PaidCounterAmount,
                                     CreatedAt,
                                     CreatedByUserId
                                 )
                                 VALUES
                                 (
                                     @TransactionTypeId,
                                     0,
                                     @PurchaseNumber,
                                     @PurchaseNumber,
                                     @PurchaseDate,
                                     @SupplierId,
                                     @UsedCarId,
                                     @TotalBaseAmount,
                                     @PaidAmount,
                                     @PaymentStatus,
                                     @Notes,
                                     @PostingStatus,
                                     @PostedAt,
                                     @PostedByUserId,
                                     @BaseCurrencyCode,
                                     @CounterCurrencyCode,
                                     @TotalBaseAmount,
                                     @TotalCounterAmount,
                                     @PaidCounterAmount,
                                     @CreatedAt,
                                     @CreatedByUserId
                                 );

                                 DECLARE @TransactionId INT = CAST(SCOPE_IDENTITY() AS INT);
                                 UPDATE dbo.Transactions
                                 SET ReferenceId = @TransactionId
                                 WHERE Id = @TransactionId;

                                 SELECT @TransactionId;";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.UsedCarPurchase,
                    purchase.PurchaseNumber,
                    purchase.UsedCarId,
                    purchase.SupplierId,
                    purchase.PurchaseDate,
                    purchase.BaseCurrencyCode,
                    purchase.CounterCurrencyCode,
                    purchase.TotalBaseAmount,
                    purchase.TotalCounterAmount,
                    purchase.PaidAmount,
                    purchase.PaidCounterAmount,
                    purchase.PaymentStatus,
                    purchase.PostingStatus,
                    purchase.PostedAt,
                    purchase.PostedByUserId,
                    purchase.Notes,
                    purchase.CreatedAt,
                    purchase.CreatedByUserId
                },
                _session.Transaction);
        }

        public void InsertLines(int purchaseId, IList<UsedCarPurchaseLine> lines)
        {
            var transactionId = ResolveTransactionId(purchaseId);

            const string sql = @"INSERT INTO dbo.TransactionItems
                                 (
                                     TransactionId,
                                     ItemType,
                                     AccountId,
                                     DetailKey,
                                     Description,
                                     Amount,
                                     LineTotal,
                                     CurrencyCode,
                                     RateToBase,
                                     BaseAmount,
                                     CounterAmount,
                                     SortOrder,
                                     CreatedAt,
                                     CreatedByUserId,
                                     ModifiedAt,
                                     ModifiedByUserId
                                 )
                                 VALUES
                                 (
                                     @TransactionId,
                                     @ItemType,
                                     @AccountId,
                                     @DetailKey,
                                     @Description,
                                     @Amount,
                                     @BaseAmount,
                                     @CurrencyCode,
                                     @RateToBase,
                                     @BaseAmount,
                                     @CounterAmount,
                                     @SortOrder,
                                     @CreatedAt,
                                     @CreatedByUserId,
                                     @ModifiedAt,
                                     @ModifiedByUserId
                                 );";

            foreach (var line in lines)
            {
                line.UsedCarPurchaseId = purchaseId;
                _session.Connection.Execute(sql, new
                {
                    TransactionId = transactionId,
                    ItemType = "used_car_purchase_line",
                    line.AccountId,
                    line.DetailKey,
                    line.Description,
                    line.Amount,
                    line.CurrencyCode,
                    line.RateToBase,
                    line.BaseAmount,
                    line.CounterAmount,
                    line.SortOrder,
                    line.CreatedAt,
                    line.CreatedByUserId,
                    line.ModifiedAt,
                    line.ModifiedByUserId
                }, _session.Transaction);
            }
        }

        public IReadOnlyList<UsedCarPurchaseSummaryDto> GetAll()
        {
            const string sql = @"SELECT t.ReferenceId AS Id,
                                        t.TransactionNumber AS PurchaseNumber,
                                        t.ScanCode,
                                        t.TransactionDate AS PurchaseDate,
                                        t.UsedCarId,
                                        CASE
                                            WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                                                CASE
                                                    WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                                                    ELSE cm.Name + N' (' + cm.BodyType + N')'
                                                END
                                            ELSE
                                                CASE
                                                    WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                                                    ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                                                END
                                        END + N' ' + CONVERT(NVARCHAR(4), uc.ModelYear) AS UsedCar,
                                        t.SupplierId,
                                        s.Name AS SupplierName,
                                        ISNULL(NULLIF(t.BaseCurrencyCode, N''), N'USD') AS BaseCurrencyCode,
                                        ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD') AS CounterCurrencyCode,
                                        ISNULL(t.TotalBaseAmount, 0) AS TotalBaseAmount,
                                        ISNULL(t.TotalCounterAmount, 0) AS TotalCounterAmount,
                                        ISNULL(t.PaidAmount, 0) AS PaidAmount,
                                        ISNULL(t.PaidCounterAmount, 0) AS PaidCounterAmount,
                                        ISNULL(t.PaymentStatus, N'Unpaid') AS PaymentStatus,
                                        ISNULL(t.PostingStatus, N'Draft') AS PostingStatus,
                                        t.PostedAt,
                                        COUNT(l.Id) AS LineCount
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 INNER JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
                                 INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
                                 INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
                                 INNER JOIN dbo.Suppliers s ON s.Id = t.SupplierId
                                 LEFT JOIN dbo.TransactionItems l ON l.TransactionId = t.Id
                                 WHERE tt.TypeKey = @TypeKey
                                 GROUP BY t.ReferenceId,
                                          t.TransactionNumber,
                                          t.ScanCode,
                                          t.TransactionDate,
                                          t.UsedCarId,
                                          cb.Name,
                                          cm.Name,
                                          cm.BodyType,
                                          uc.ModelYear,
                                          t.SupplierId,
                                          s.Name,
                                          t.BaseCurrencyCode,
                                          t.CounterCurrencyCode,
                                          t.TotalBaseAmount,
                                          t.TotalCounterAmount,
                                          t.PaidAmount,
                                          t.PaidCounterAmount,
                                          t.PaymentStatus,
                                          t.PostingStatus,
                                          t.PostedAt
                                 ORDER BY t.TransactionDate DESC, t.ReferenceId DESC;";

            return _session.Connection.Query<UsedCarPurchaseSummaryDto>(
                sql,
                new { TypeKey = TransactionTypeKeys.UsedCarPurchase },
                _session.Transaction).ToList();
        }

        public UsedCarPurchaseDetailDto? GetDetail(int id)
            => GetDetailInternal("t.ReferenceId = @Id", new { Id = id });

        public UsedCarPurchaseDetailDto? GetDraftByUsedCarId(int usedCarId)
            => GetDetailInternal(
                "t.UsedCarId = @UsedCarId AND ISNULL(t.PostingStatus, N'Draft') <> N'Posted'",
                new { UsedCarId = usedCarId });

        public bool Update(int id, UsedCarPurchase purchase)
        {
            const string sql = @"UPDATE t
                                 SET SupplierId = @SupplierId,
                                     TransactionDate = @PurchaseDate,
                                     BaseCurrencyCode = @BaseCurrencyCode,
                                     CounterCurrencyCode = @CounterCurrencyCode,
                                     TotalAmount = @TotalBaseAmount,
                                     TotalBaseAmount = @TotalBaseAmount,
                                     TotalCounterAmount = @TotalCounterAmount,
                                     PaidAmount = @PaidAmount,
                                     PaidCounterAmount = @PaidCounterAmount,
                                     PaymentStatus = @PaymentStatus,
                                     PostingStatus = @PostingStatus,
                                     PostedAt = @PostedAt,
                                     PostedByUserId = @PostedByUserId,
                                     Notes = @Notes,
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @Id
                                   AND ISNULL(t.PostingStatus, N'Draft') <> N'Posted';";

            return _session.Connection.Execute(sql, new
            {
                TypeKey = TransactionTypeKeys.UsedCarPurchase,
                Id = id,
                purchase.SupplierId,
                purchase.PurchaseDate,
                purchase.BaseCurrencyCode,
                purchase.CounterCurrencyCode,
                purchase.TotalBaseAmount,
                purchase.TotalCounterAmount,
                purchase.PaidAmount,
                purchase.PaidCounterAmount,
                purchase.PaymentStatus,
                purchase.PostingStatus,
                purchase.PostedAt,
                purchase.PostedByUserId,
                purchase.Notes,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = purchase.CreatedByUserId
            }, _session.Transaction) > 0;
        }

        public void ReplaceLines(int purchaseId, IList<UsedCarPurchaseLine> lines)
        {
            var transactionId = ResolveTransactionId(purchaseId);

            const string deleteSql = @"DELETE FROM dbo.TransactionItems
                                       WHERE TransactionId = @TransactionId;";

            _session.Connection.Execute(deleteSql, new { TransactionId = transactionId }, _session.Transaction);
            InsertLines(purchaseId, lines);
        }

        public bool MarkPosted(int id, DateTime postedAt, int postedByUserId)
        {
            const string sql = @"UPDATE t
                                 SET PostingStatus = N'Posted',
                                     PostedAt = @PostedAt,
                                     PostedByUserId = @PostedByUserId,
                                     ModifiedAt = @PostedAt,
                                     ModifiedByUserId = @PostedByUserId
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @Id
                                   AND ISNULL(t.PostingStatus, N'Draft') <> N'Posted';";

            return _session.Connection.Execute(sql, new
            {
                TypeKey = TransactionTypeKeys.UsedCarPurchase,
                Id = id,
                PostedAt = postedAt,
                PostedByUserId = postedByUserId
            }, _session.Transaction) > 0;
        }

        public bool HasPostedPurchase(int usedCarId)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.UsedCarId = @UsedCarId
                                   AND ISNULL(t.PostingStatus, N'Draft') = N'Posted';";

            return _session.Connection.ExecuteScalar<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.UsedCarPurchase,
                    UsedCarId = usedCarId
                },
                _session.Transaction) > 0;
        }

        public int DeleteDraftsByUsedCarId(int usedCarId)
        {
            const string sql = @"DELETE t
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.UsedCarId = @UsedCarId
                                   AND ISNULL(t.PostingStatus, N'Draft') <> N'Posted';";

            return _session.Connection.Execute(sql, new
            {
                TypeKey = TransactionTypeKeys.UsedCarPurchase,
                UsedCarId = usedCarId
            }, _session.Transaction);
        }

        public bool Delete(int id)
        {
            const string sql = @"DELETE t
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @Id;";

            return _session.Connection.Execute(sql, new
            {
                TypeKey = TransactionTypeKeys.UsedCarPurchase,
                Id = id
            }, _session.Transaction) > 0;
        }

        private UsedCarPurchaseDetailDto? GetDetailInternal(string whereClause, object parameters)
        {
            var queryParameters = new DynamicParameters(parameters);
            queryParameters.Add("TypeKey", TransactionTypeKeys.UsedCarPurchase);

            var headerSql = $@"SELECT TOP (1)
                                      t.ReferenceId AS Id,
                                      t.TransactionNumber AS PurchaseNumber,
                                      t.ScanCode,
                                      t.TransactionDate AS PurchaseDate,
                                      t.UsedCarId,
                                      CASE
                                          WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                                              CASE
                                                  WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                                                  ELSE cm.Name + N' (' + cm.BodyType + N')'
                                              END
                                          ELSE
                                              CASE
                                                  WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                                                  ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                                              END
                                      END + N' ' + CONVERT(NVARCHAR(4), uc.ModelYear) AS UsedCar,
                                      t.SupplierId,
                                      s.Name AS SupplierName,
                                      ISNULL(NULLIF(t.BaseCurrencyCode, N''), N'USD') AS BaseCurrencyCode,
                                      ISNULL(NULLIF(t.CounterCurrencyCode, N''), N'USD') AS CounterCurrencyCode,
                                      ISNULL(t.TotalBaseAmount, 0) AS TotalBaseAmount,
                                      ISNULL(t.TotalCounterAmount, 0) AS TotalCounterAmount,
                                      ISNULL(t.PaidAmount, 0) AS PaidAmount,
                                      ISNULL(t.PaidCounterAmount, 0) AS PaidCounterAmount,
                                      ISNULL(t.PaymentStatus, N'Unpaid') AS PaymentStatus,
                                      ISNULL(t.PostingStatus, N'Draft') AS PostingStatus,
                                      t.PostedAt,
                                      ISNULL(t.Notes, N'') AS Notes,
                                      COUNT(l.Id) AS LineCount
                               FROM dbo.Transactions t
                               INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                               INNER JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
                               INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
                               INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
                               INNER JOIN dbo.Suppliers s ON s.Id = t.SupplierId
                               LEFT JOIN dbo.TransactionItems l ON l.TransactionId = t.Id
                               WHERE tt.TypeKey = @TypeKey
                                 AND {whereClause}
                               GROUP BY t.ReferenceId,
                                        t.TransactionNumber,
                                        t.ScanCode,
                                        t.TransactionDate,
                                        t.UsedCarId,
                                        cb.Name,
                                        cm.Name,
                                        cm.BodyType,
                                        uc.ModelYear,
                                        t.SupplierId,
                                        s.Name,
                                        t.BaseCurrencyCode,
                                        t.CounterCurrencyCode,
                                        t.TotalBaseAmount,
                                        t.TotalCounterAmount,
                                        t.PaidAmount,
                                        t.PaidCounterAmount,
                                        t.PaymentStatus,
                                        t.PostingStatus,
                                        t.PostedAt,
                                        t.Notes
                               ORDER BY t.ReferenceId DESC;";

            const string linesSql = @"SELECT ti.Id,
                                             ISNULL(ti.DetailKey, N'') AS DetailKey,
                                             ISNULL(ti.Description, N'') AS Description,
                                             ISNULL(ti.Amount, 0) AS Amount,
                                             ISNULL(NULLIF(ti.CurrencyCode, N''), N'USD') AS CurrencyCode,
                                             ISNULL(ti.RateToBase, 1) AS RateToBase,
                                             ISNULL(ti.BaseAmount, 0) AS BaseAmount,
                                             ISNULL(ti.CounterAmount, 0) AS CounterAmount,
                                             ti.AccountId,
                                             a.Code AS AccountCode,
                                             a.Name AS AccountName,
                                             ti.SortOrder
                                      FROM dbo.TransactionItems ti
                                      INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
                                      INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                      INNER JOIN dbo.Accounts a ON a.Id = ti.AccountId
                                      WHERE tt.TypeKey = @TypeKey
                                        AND t.ReferenceId = @Id
                                      ORDER BY ti.SortOrder, ti.Id;";

            var detail = _session.Connection.QueryFirstOrDefault<UsedCarPurchaseDetailDto>(
                headerSql,
                queryParameters,
                _session.Transaction);

            if (detail == null)
            {
                return null;
            }

            detail.Lines = _session.Connection.Query<UsedCarPurchaseLineDetailDto>(
                linesSql,
                new
                {
                    TypeKey = TransactionTypeKeys.UsedCarPurchase,
                    detail.Id
                },
                _session.Transaction).ToList();
            detail.Timeline = new TransactionTimelineReader(_session).Build(TransactionTypeKeys.UsedCarPurchase, detail.Id);

            return detail;
        }

        private int ResolveTransactionId(int purchaseId)
        {
            const string sql = @"SELECT t.Id
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @PurchaseId;";

            var transactionId = _session.Connection.QuerySingleOrDefault<int>(
                sql,
                new
                {
                    TypeKey = TransactionTypeKeys.UsedCarPurchase,
                    PurchaseId = purchaseId
                },
                _session.Transaction);

            if (transactionId <= 0)
            {
                throw new InvalidOperationException($"Used-car purchase transaction {purchaseId} was not found.");
            }

            return transactionId;
        }
    }
}
