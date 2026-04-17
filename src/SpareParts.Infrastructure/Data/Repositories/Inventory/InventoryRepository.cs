using Dapper;
using SpareParts.Domain.Inventory;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

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

        public bool TryUpdateStockQuantityAtomically(int stockId, int delta, int userId)
        {
            const string sql = @"UPDATE Stock
                                 SET Quantity = Quantity + @Delta,
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 WHERE Id = @Id
                                   AND Quantity + @Delta >= 0";

            var affectedRows = _session.Connection.Execute(sql, new
            {
                Id = stockId,
                Delta = delta,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId
            }, _session.Transaction);

            return affectedRows > 0;
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
