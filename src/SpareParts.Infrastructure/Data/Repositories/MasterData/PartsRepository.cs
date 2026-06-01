using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;

using SpareParts.Infrastructure.Interfaces.Repositories;
using System.Data;

namespace SpareParts.Infrastructure.Data
{

    public class PartsRepository : IPartsRepository
    {
        private readonly DbSession _session;

        public PartsRepository(DbSession session)
        {
            _session = session;
        }

        public Dictionary<int, Part> GetByIds(IList<int> partIds)
        {
            if (partIds.Count == 0)
            {
                return new Dictionary<int, Part>();
            }

            const string sql = """
SELECT p.*
FROM dbo.Parts p
INNER JOIN @Ids ids ON ids.Id = p.Id;
""";
            return _session.Connection.Query<Part>(
                    sql,
                    new { Ids = ToIntIdList(partIds).AsTableValuedParameter("dbo.IntIdList") },
                    _session.Transaction)
                .ToDictionary(p => p.Id, p => p);
        }

        public IEnumerable<Part> GetAllActive(int? usedCarId = null)
        {
            const string sql = @"SELECT *
                                 FROM Parts
                                 WHERE IsActive = 1
                                   AND (@UsedCarId IS NULL OR UsedCarId = @UsedCarId)
                                 ORDER BY Name";
            return _session.Connection.Query<Part>(sql, new { UsedCarId = usedCarId }, _session.Transaction);
        }

        public int Insert(Part part)
        {
            const string sql = @"INSERT INTO Parts
                (InternalCode, Barcode, Name, OEMNumber, Condition, CategoryId, BrandId,
                 CostPrice, SalePrice, AveragePrice, EstimatedMarketPrice, CostAllocationPercent, AllocatedCost,
                 MinimumSellPrice, FastSalePrice, WholesalePrice, RecommendedPrice, PricingStatus, PricingCalculatedAt,
                 Currency, MinStock, Notes, UsedCarId, IsActive, CreatedAt, CreatedByUserId)
                VALUES
                (@InternalCode, @Barcode, @Name, @OEMNumber, @Condition, @CategoryId, @BrandId,
                 @CostPrice, @SalePrice, @AveragePrice, @EstimatedMarketPrice, @CostAllocationPercent, @AllocatedCost,
                 @MinimumSellPrice, @FastSalePrice, @WholesalePrice, @RecommendedPrice, @PricingStatus, @PricingCalculatedAt,
                 @Currency, @MinStock, @Notes, @UsedCarId, @IsActive, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, part, _session.Transaction);
        }

        public bool Update(int id, CreatePartRequest request, int userId)
        {
            const string sql = @"UPDATE Parts
                                 SET InternalCode = @InternalCode, Barcode = @Barcode, Name = @Name,
                                     OEMNumber = @OEMNumber, Condition = @Condition, CategoryId = @CategoryId, BrandId = @BrandId,
                                     CostPrice = @CostPrice, SalePrice = @SalePrice, AveragePrice = @AveragePrice,
                                     EstimatedMarketPrice = @EstimatedMarketPrice, CostAllocationPercent = @CostAllocationPercent,
                                     AllocatedCost = @AllocatedCost, MinimumSellPrice = @MinimumSellPrice, FastSalePrice = @FastSalePrice,
                                     WholesalePrice = @WholesalePrice, RecommendedPrice = @RecommendedPrice,
                                     PricingStatus = @PricingStatus, PricingCalculatedAt = @PricingCalculatedAt,
                                     Currency = @Currency, MinStock = @MinStock,
                                     Notes = @Notes, UsedCarId = @UsedCarId, ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                request.InternalCode,
                Barcode = NormalizeOptional(request.Barcode),
                request.Name,
                request.OEMNumber,
                request.Condition,
                request.CategoryId,
                request.BrandId,
                request.CostPrice,
                request.SalePrice,
                request.AveragePrice,
                request.EstimatedMarketPrice,
                request.CostAllocationPercent,
                request.AllocatedCost,
                request.MinimumSellPrice,
                request.FastSalePrice,
                request.WholesalePrice,
                request.RecommendedPrice,
                PricingStatus = NormalizePricingStatus(request.PricingStatus),
                request.PricingCalculatedAt,
                request.Currency,
                request.MinStock,
                request.Notes,
                request.UsedCarId,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool UpdateUsedCarId(int id, int? usedCarId, int userId)
        {
            const string sql = @"UPDATE Parts
                                 SET UsedCarId = @UsedCarId,
                                     ModifiedAt = @Now,
                                     ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                UsedCarId = usedCarId,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Parts WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string NormalizePricingStatus(string? value)
            => string.IsNullOrWhiteSpace(value) ? "Manual" : value.Trim();

        private static DataTable ToIntIdList(IEnumerable<int> ids)
        {
            var table = new DataTable();
            table.Columns.Add("Id", typeof(int));

            foreach (var id in ids.Distinct())
            {
                table.Rows.Add(id);
            }

            return table;
        }
    }
}
