using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Interfaces
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
            DomainReferenceType referenceType,
            int? referenceId,
            decimal unitCost,
            int userId);
    }
}
