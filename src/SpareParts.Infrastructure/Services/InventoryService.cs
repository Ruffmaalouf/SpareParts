using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class InventoryService
    {
        private readonly SparePartsDataContext _ctx;

        public InventoryService(SparePartsDataContext ctx)
        {
            _ctx = ctx;
        }

        public int GetAvailableStock(int partId, int warehouseId)
        {
            var stock = _ctx.GetStock(partId, warehouseId);
            return stock?.Quantity ?? 0;
        }

        public void AdjustStock(
            int partId,
            int warehouseId,
            int quantityChange,
            StockMovementType movementType,
            string referenceType,
            int? referenceId,
            decimal unitCost,
            int userId)
        {
            var existing = _ctx.GetStock(partId, warehouseId);
            if (existing == null)
            {
                var stock = new Stock
                {
                    PartId = partId,
                    WarehouseId = warehouseId,
                    Quantity = quantityChange,
                    ReservedQuantity = 0,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                };
                _ctx.InsertStock(stock);
            }
            else
            {
                _ctx.UpdateStockQuantity(existing.Id, quantityChange, userId);
            }

            var movement = new StockMovement
            {
                PartId = partId,
                WarehouseId = warehouseId,
                Quantity = quantityChange,
                MovementType = movementType,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                UnitCost = unitCost,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };
            _ctx.InsertStockMovement(movement);
        }
    }
}
