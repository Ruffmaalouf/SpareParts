using Dapper;
using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Data
{
    public interface IInventoryRepository
    {
        Stock? GetStock(int partId, int warehouseId);
        int InsertStock(Stock stock);
        void UpdateStockQuantity(int stockId, int delta, int userId);
        int InsertStockMovement(StockMovement movement);
    }

    public class InventoryRepository : IInventoryRepository
    {
        private readonly DbSession _session;

        public InventoryRepository(DbSession session)
        {
            _session = session;
        }

        public Stock? GetStock(int partId, int warehouseId)
        {
            const string sql = "SELECT TOP 1 * FROM Stock WHERE PartId = @PartId AND WarehouseId = @WarehouseId";
            return _session.Connection.QueryFirstOrDefault<Stock>(sql, new { PartId = partId, WarehouseId = warehouseId }, _session.Transaction);
        }

        public int InsertStock(Stock stock)
        {
            const string sql = @"INSERT INTO Stock
                (PartId, WarehouseId, LocationId, Quantity, ReservedQuantity, CreatedAt, CreatedByUserId)
                VALUES
                (@PartId, @WarehouseId, @LocationId, @Quantity, @ReservedQuantity, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, stock, _session.Transaction);
        }

        public void UpdateStockQuantity(int stockId, int delta, int userId)
        {
            const string sql = @"UPDATE Stock
                                 SET Quantity = Quantity + @Delta,
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 WHERE Id = @Id";
            _session.Connection.Execute(sql, new
            {
                Id = stockId,
                Delta = delta,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId
            }, _session.Transaction);
        }

        public int InsertStockMovement(StockMovement movement)
        {
            const string sql = @"INSERT INTO StockMovements
                (PartId, WarehouseId, Quantity, MovementType, ReferenceType, ReferenceId,
                 UnitCost, CreatedAt, CreatedByUserId)
                VALUES
                (@PartId, @WarehouseId, @Quantity, @MovementType, @ReferenceType, @ReferenceId,
                 @UnitCost, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, movement, _session.Transaction);
        }
    }
}
