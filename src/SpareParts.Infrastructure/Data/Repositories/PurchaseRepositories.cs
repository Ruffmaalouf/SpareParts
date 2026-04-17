using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class PurchaseRepositories
    {
        internal PurchaseRepositories(DbSession session)
        {
            Invoices = new PurchasesRepository(session);
            UsedCarPurchases = new UsedCarPurchasesRepository(session);
        }

        public IPurchasesRepository Invoices { get; }
        public IUsedCarPurchasesRepository UsedCarPurchases { get; }
    }
}
