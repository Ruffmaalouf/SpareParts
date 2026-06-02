using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.ArchitectureTests.TestDoubles;

internal sealed class ThrowOnMovementInsertInventoryRepository : IInventoryRepository
{
    private readonly List<Stock> _stocks = new();

    public Stock? GetStock(int partId, int warehouseId)
        => _stocks.FirstOrDefault(x => x.PartId == partId && x.WarehouseId == warehouseId);

    public int InsertStock(Stock stock)
    {
        stock.Id = _stocks.Count + 1;
        _stocks.Add(stock);
        return stock.Id;
    }

    public void UpdateStockQuantity(int stockId, int delta, int userId)
    {
        var stock = _stocks.First(x => x.Id == stockId);
        stock.Quantity += delta;
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedByUserId = userId;
    }

    public bool TryUpdateStockQuantityAtomically(int stockId, int delta, int userId)
    {
        var stock = _stocks.First(x => x.Id == stockId);
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
        var stock = _stocks.First(x => x.Id == stockId);
        if (stock.Quantity - quantityToSell < 0) return false;
        stock.Quantity -= quantityToSell;
        stock.ReservedQuantity = Math.Max(0, stock.ReservedQuantity - quantityToSell);
        stock.ModifiedAt = DateTime.UtcNow;
        stock.ModifiedByUserId = userId;
        return true;
    }

    public int InsertStockMovement(StockMovement movement)
        => throw new InvalidOperationException("Simulated movement insert failure.");
}
