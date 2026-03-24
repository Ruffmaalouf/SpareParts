using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public interface IInventoryService
    {
        int GetAvailableStock(SparePartsDataContext ctx, int partId, int warehouseId);

        void AdjustStock(
            SparePartsDataContext ctx,
            int partId,
            int warehouseId,
            int quantityChange,
            StockMovementType movementType,
            string referenceType,
            int? referenceId,
            decimal unitCost,
            int userId);
    }

    public class InventoryService : IInventoryService
    {
        public int GetAvailableStock(SparePartsDataContext ctx, int partId, int warehouseId)
        {
            var stock = ctx.GetStock(partId, warehouseId);
            return stock?.Quantity ?? 0;
        }

        public void AdjustStock(
            SparePartsDataContext ctx,
            int partId,
            int warehouseId,
            int quantityChange,
            StockMovementType movementType,
            string referenceType,
            int? referenceId,
            decimal unitCost,
            int userId)
        {
            var existing = ctx.GetStock(partId, warehouseId);
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
                ctx.InsertStock(stock);
            }
            else
            {
                ctx.UpdateStockQuantity(existing.Id, quantityChange, userId);
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
            ctx.InsertStockMovement(movement);
        }
    }
}
