using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public interface IInventoryService
    {
        int GetAvailableStock(IInventoryRepository inventoryRepository, int partId, int warehouseId);

        void AdjustStock(
            IInventoryRepository inventoryRepository,
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
        public int GetAvailableStock(IInventoryRepository inventoryRepository, int partId, int warehouseId)
        {
            var stock = inventoryRepository.GetStock(partId, warehouseId);
            return stock?.Quantity ?? 0;
        }

        public void AdjustStock(
            IInventoryRepository inventoryRepository,
            int partId,
            int warehouseId,
            int quantityChange,
            StockMovementType movementType,
            string referenceType,
            int? referenceId,
            decimal unitCost,
            int userId)
        {
            var existing = inventoryRepository.GetStock(partId, warehouseId);
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
                inventoryRepository.InsertStock(stock);
            }
            else
            {
                inventoryRepository.UpdateStockQuantity(existing.Id, quantityChange, userId);
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
            inventoryRepository.InsertStockMovement(movement);
        }
    }
}
