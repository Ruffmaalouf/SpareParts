using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Services
{
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
            var stockId = existing?.Id ?? 0;

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

                stockId = inventoryRepository.InsertStock(stock);
            }
            else
            {
                inventoryRepository.UpdateStockQuantity(stockId, quantityChange, userId);
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

            try
            {
                inventoryRepository.InsertStockMovement(movement);
            }
            catch
            {
                if (stockId > 0 && quantityChange != 0)
                {
                    inventoryRepository.UpdateStockQuantity(stockId, -quantityChange, userId);
                }

                throw;
            }
        }
    }
}
