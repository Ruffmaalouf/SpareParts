namespace SpareParts.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Centralized repository composition for transaction-scoped workflows.
    /// Keeps repository organization discoverable by business capability.
    /// </summary>
    public sealed class RepositoryCatalog
    {
        private RepositoryCatalog(DbSession session)
        {
            Sales = new SalesRepositories(session);
            Purchases = new PurchaseRepositories(session);
            Inventory = new InventoryRepositories(session);
            Accounting = new AccountingRepositories(session);
            MasterData = new MasterDataRepositories(session);
        }

        public SalesRepositories Sales { get; }
        public PurchaseRepositories Purchases { get; }
        public InventoryRepositories Inventory { get; }
        public AccountingRepositories Accounting { get; }
        public MasterDataRepositories MasterData { get; }

        public static RepositoryCatalog For(DbSession session) => new(session);
    }
}
