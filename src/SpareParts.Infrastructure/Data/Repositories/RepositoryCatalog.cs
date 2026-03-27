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

        public sealed class SalesRepositories
        {
            internal SalesRepositories(DbSession session) => Invoices = new SalesRepository(session);
            public ISalesRepository Invoices { get; }
        }

        public sealed class PurchaseRepositories
        {
            internal PurchaseRepositories(DbSession session) => Invoices = new PurchasesRepository(session);
            public IPurchasesRepository Invoices { get; }
        }

        public sealed class InventoryRepositories
        {
            internal InventoryRepositories(DbSession session) => Stock = new InventoryRepository(session);
            public IInventoryRepository Stock { get; }
        }

        public sealed class AccountingRepositories
        {
            internal AccountingRepositories(DbSession session) => Journal = new JournalRepository(session);
            public IJournalRepository Journal { get; }
        }

        public sealed class MasterDataRepositories
        {
            internal MasterDataRepositories(DbSession session) => Parts = new PartsRepository(session);
            public IPartsRepository Parts { get; }
        }
    }
}
