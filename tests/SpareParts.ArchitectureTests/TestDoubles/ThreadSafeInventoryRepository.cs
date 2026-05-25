using System.Collections.Concurrent;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.ArchitectureTests.TestDoubles;

internal sealed class ThreadSafeInventoryRepository : IInventoryRepository
{
    private readonly ConcurrentDictionary<(int PartId, int WarehouseId), Stock> _stocks = new();
    private readonly ConcurrentDictionary<int, Stock> _stocksById = new();
    private readonly ConcurrentQueue<StockMovement> _movements = new();
    private int _stockIdSeed;
    private int _movementIdSeed;
    private readonly object _stockLock = new();

    public int MovementCount => _movements.Count;

    public Stock? GetStock(int partId, int warehouseId)
    {
        _stocks.TryGetValue((partId, warehouseId), out var stock);
        return stock;
    }

    public int InsertStock(Stock stock)
    {
        lock (_stockLock)
        {
            var existing = GetStock(stock.PartId, stock.WarehouseId);
            if (existing != null)
            {
                return existing.Id;
            }

            var id = Interlocked.Increment(ref _stockIdSeed);
            stock.Id = id;
            _stocks[(stock.PartId, stock.WarehouseId)] = stock;
            _stocksById[id] = stock;
            return id;
        }
    }

    public void UpdateStockQuantity(int stockId, int delta, int userId)
    {
        lock (_stockLock)
        {
            var stock = _stocksById[stockId];
            stock.Quantity += delta;
            stock.ModifiedAt = DateTime.UtcNow;
            stock.ModifiedByUserId = userId;
        }
    }

    public bool TryUpdateStockQuantityAtomically(int stockId, int delta, int userId)
    {
        lock (_stockLock)
        {
            var stock = _stocksById[stockId];
            if (stock.Quantity + delta < stock.ReservedQuantity)
            {
                return false;
            }

            stock.Quantity += delta;
            stock.ModifiedAt = DateTime.UtcNow;
            stock.ModifiedByUserId = userId;
            return true;
        }
    }

    public int InsertStockMovement(StockMovement movement)
    {
        movement.Id = Interlocked.Increment(ref _movementIdSeed);
        _movements.Enqueue(movement);
        return movement.Id;
    }
}
