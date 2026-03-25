using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IInventoryRepository
    {
        Stock? GetStock(int partId, int warehouseId);
        int InsertStock(Stock stock);
        void UpdateStockQuantity(int stockId, int delta, int userId);
        int InsertStockMovement(StockMovement movement);
    }
}
