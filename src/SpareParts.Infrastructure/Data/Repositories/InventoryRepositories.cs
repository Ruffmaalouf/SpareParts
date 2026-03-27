namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class InventoryRepositories
    {
        internal InventoryRepositories(DbSession session) => Stock = new InventoryRepository(session);
        public IInventoryRepository Stock { get; }
    }
}
