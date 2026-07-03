using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Services
{
    public sealed class InventoryService : IInventoryService
    {
        public int GetAvailableStock(IInventoryRepository inventoryRepository, int partId, int warehouseId)
        {
            var stock = inventoryRepository.GetStock(partId, warehouseId);
            return stock == null
                ? 0
                : Math.Max(0, stock.Quantity - stock.ReservedQuantity);
        }

        public void AdjustStock(
            IInventoryRepository inventoryRepository,
            int partId,
            int warehouseId,
            int quantityChange,
            StockMovementType movementType,
            DomainReferenceType referenceType,
            int? referenceId,
            decimal unitCost,
            int userId)
        {
            if (quantityChange == 0)
            {
                return;
            }

            var existing = inventoryRepository.GetStock(partId, warehouseId);
            var resultingQuantity = (existing?.Quantity ?? 0) + quantityChange;

            if (resultingQuantity < 0)
            {
                throw new ConflictException($"Cannot reduce stock below zero for part {partId} in warehouse {warehouseId}.");
            }

            var stockId = existing?.Id ?? 0;

            if (existing == null)
            {
                if (quantityChange < 0)
                {
                    throw new ConflictException($"Cannot reduce stock below zero for part {partId} in warehouse {warehouseId}.");
                }

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
                if (quantityChange < 0)
                {
                    var updated = inventoryRepository.TryUpdateStockQuantityAtomically(stockId, quantityChange, userId);
                    if (!updated)
                    {
                        throw new ConflictException($"Cannot reduce stock below zero for part {partId} in warehouse {warehouseId}.");
                    }
                }
                else
                {
                    inventoryRepository.UpdateStockQuantity(stockId, quantityChange, userId);
                }
            }

            var movement = new StockMovement
            {
                PartId = partId,
                WarehouseId = warehouseId,
                Quantity = quantityChange,
                MovementType = movementType,
                ReferenceType = referenceType.ToString(),
                ReferenceId = referenceId,
                UnitCost = unitCost,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            // No manual "undo" here: AdjustStock runs inside the caller's ambient DbSession/
            // transaction. If InsertStockMovement throws, DbSession.Dispose() (or an explicit
            // Rollback()) rolls back every statement issued on this connection/transaction so
            // far — including the stock quantity update above — so a manual compensating write
            // would be redundant and could even double-mutate the row if the transaction is
            // later retried. Just let the exception propagate and rely on the transaction.
            inventoryRepository.InsertStockMovement(movement);
        }
    }
}
