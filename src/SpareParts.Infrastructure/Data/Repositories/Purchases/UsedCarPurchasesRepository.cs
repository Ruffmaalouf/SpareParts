using Dapper;
using SpareParts.Domain.Purchases;
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
            const string sql = @"INSERT INTO dbo.UsedCarPurchases
                (PurchaseNumber, UsedCarId, SupplierId, PurchaseDate, BaseCurrencyCode, TotalBaseAmount, PaidAmount, PaymentStatus, Notes, CreatedAt, CreatedByUserId)
                VALUES
                (@PurchaseNumber, @UsedCarId, @SupplierId, @PurchaseDate, @BaseCurrencyCode, @TotalBaseAmount, @PaidAmount, @PaymentStatus, @Notes, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return _session.Connection.ExecuteScalar<int>(sql, purchase, _session.Transaction);
        }

        public void InsertLines(int purchaseId, IList<UsedCarPurchaseLine> lines)
        {
            const string sql = @"INSERT INTO dbo.UsedCarPurchaseLines
                (UsedCarPurchaseId, DetailKey, Description, Amount, CurrencyCode, RateToBase, BaseAmount, AccountId, SortOrder, CreatedAt, CreatedByUserId)
                VALUES
                (@UsedCarPurchaseId, @DetailKey, @Description, @Amount, @CurrencyCode, @RateToBase, @BaseAmount, @AccountId, @SortOrder, @CreatedAt, @CreatedByUserId);";

            foreach (var line in lines)
            {
                line.UsedCarPurchaseId = purchaseId;
                _session.Connection.Execute(sql, line, _session.Transaction);
            }
        }

        public IReadOnlyList<UsedCarPurchaseSummaryDto> GetAll()
        {
            const string sql = @"SELECT p.Id,
                                        p.PurchaseNumber,
                                        p.PurchaseDate,
                                        p.UsedCarId,
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
                                        p.SupplierId,
                                        s.Name AS SupplierName,
                                        p.BaseCurrencyCode,
                                        p.TotalBaseAmount,
                                        p.PaidAmount,
                                        p.PaymentStatus,
                                        COUNT(l.Id) AS LineCount
                                 FROM dbo.UsedCarPurchases p
                                 INNER JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
                                 INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
                                 INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
                                 INNER JOIN dbo.Suppliers s ON s.Id = p.SupplierId
                                 LEFT JOIN dbo.UsedCarPurchaseLines l ON l.UsedCarPurchaseId = p.Id
                                 GROUP BY p.Id,
                                          p.PurchaseNumber,
                                          p.PurchaseDate,
                                          p.UsedCarId,
                                          cb.Name,
                                          cm.Name,
                                          cm.BodyType,
                                          uc.ModelYear,
                                          p.SupplierId,
                                          s.Name,
                                          p.BaseCurrencyCode,
                                          p.TotalBaseAmount,
                                          p.PaidAmount,
                                          p.PaymentStatus
                                 ORDER BY p.PurchaseDate DESC, p.Id DESC;";

            return _session.Connection.Query<UsedCarPurchaseSummaryDto>(sql, transaction: _session.Transaction).ToList();
        }
    }
}
