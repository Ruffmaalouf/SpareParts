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
            const string sql = "SELECT TOP 1 * FROM Stock WHERE PartId = @PartId AND WarehouseId = @WarehouseId AND (@TenantId = 0 OR TenantId = @TenantId)";
            return _session.Connection.QueryFirstOrDefault<Stock>(sql, new { PartId = partId, WarehouseId = warehouseId, _session.TenantId }, _session.Transaction);
        }

        public int InsertStock(Stock stock)
        {
            const string sql = @"INSERT INTO Stock
                (PartId, WarehouseId, LocationId, Quantity, ReservedQuantity, CreatedAt, CreatedByUserId, TenantId)
                VALUES
                (@PartId, @WarehouseId, @LocationId, @Quantity, @ReservedQuantity, @CreatedAt, @CreatedByUserId, @TenantId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            return _session.Connection.ExecuteScalar<int>(sql, new
            {
                stock.PartId,
                stock.WarehouseId,
                stock.LocationId,
                stock.Quantity,
                stock.ReservedQuantity,
                stock.CreatedAt,
                stock.CreatedByUserId,
                TenantId = tenantId
            }, _session.Transaction);
        }

        public void UpdateStockQuantity(int stockId, int delta, int userId)
        {
            const string sql = @"UPDATE Stock
                                 SET Quantity = Quantity + @Delta,
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 WHERE Id = @Id
                                   AND (@TenantId = 0 OR TenantId = @TenantId)";
            _session.Connection.Execute(sql, new
            {
                Id = stockId,
                Delta = delta,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId,
                _session.TenantId
            }, _session.Transaction);
        }

        public bool TryUpdateStockQuantityAtomically(int stockId, int delta, int userId)
        {
            const string sql = @"UPDATE Stock
                                 SET Quantity = Quantity + @Delta,
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 WHERE Id = @Id
                                   AND Quantity + @Delta >= ISNULL(ReservedQuantity, 0)
                                   AND (@TenantId = 0 OR TenantId = @TenantId)";

            var affectedRows = _session.Connection.Execute(sql, new
            {
                Id = stockId,
                Delta = delta,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId,
                _session.TenantId
            }, _session.Transaction);

            return affectedRows > 0;
        }

        public int InsertStockMovement(StockMovement movement)
        {
            const string sql = @"INSERT INTO StockMovements
                (PartId, WarehouseId, Quantity, MovementType, ScanCode, ReferenceType, ReferenceId,
                 UnitCost, CreatedAt, CreatedByUserId, TenantId)
                VALUES
                (@PartId, @WarehouseId, @Quantity, @MovementType, @ScanCode, @ReferenceType, @ReferenceId,
                 @UnitCost, @CreatedAt, @CreatedByUserId, @TenantId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            var movementId = _session.Connection.ExecuteScalar<int>(sql, new
            {
                movement.PartId,
                movement.WarehouseId,
                movement.Quantity,
                movement.MovementType,
                movement.ScanCode,
                movement.ReferenceType,
                movement.ReferenceId,
                movement.UnitCost,
                movement.CreatedAt,
                movement.CreatedByUserId,
                TenantId = tenantId
            }, _session.Transaction);
            _session.Connection.Execute(
                @"UPDATE StockMovements
                  SET ScanCode = @ScanCode
                  WHERE Id = @Id
                    AND (ScanCode IS NULL OR LTRIM(RTRIM(ScanCode)) = N'');",
                new { Id = movementId, ScanCode = $"SM-{movementId}" },
                _session.Transaction);
            return movementId;
        }
    }
}
