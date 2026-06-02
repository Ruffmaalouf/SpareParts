using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.ArchitectureTests.TestDoubles;

internal sealed class FakeInventoryRepository : IInventoryRepository
{
    public List<Stock> StockRows { get; } = new();
    public List<StockMovement> Movements { get; } = new();

    public Stock? GetStock(int partId, int warehouseId)
        => StockRows.FirstOrDefault(x => x.PartId == partId && x.WarehouseId == warehouseId);

    public int InsertStock(Stock stock)
    {
        stock.Id = StockRows.Count + 1;
        StockRows.Add(stock);
        return stock.Id;
    }

    public void UpdateStockQuantity(int stockId, int delta, int userId)
    {
        var stock = StockRows.First(x => x.Id == stockId);
        stock.Quantity += delta;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedByUserId = userId;
    }

    public bool TryUpdateStockQuantityAtomically(int stockId, int delta, int userId)
    {
        var stock = StockRows.First(x => x.Id == stockId);
        if (stock.Quantity + delta < stock.ReservedQuantity)
        {
            return false;
        }

        stock.Quantity += delta;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedByUserId = userId;
        return true;
    }

    public bool TryUpdateStockOnSale(int stockId, int quantityToSell, int userId)
    {
        var stock = StockRows.First(x => x.Id == stockId);
        if (stock.Quantity - quantityToSell < 0) return false;
        stock.Quantity -= quantityToSell;
        stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - quantityToSell);
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedByUserId = userId;
        return true;
    }

    public int InsertStockMovement(StockMovement movement)
    {
        movement.Id = Movements.Count + 1;
        Movements.Add(movement);
        return movement.Id;
    }
}
