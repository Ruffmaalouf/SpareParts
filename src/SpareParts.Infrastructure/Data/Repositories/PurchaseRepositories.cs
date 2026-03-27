namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class PurchaseRepositories
    {
        internal PurchaseRepositories(DbSession session) => Invoices = new PurchasesRepository(session);
        public IPurchasesRepository Invoices { get; }
    }
}
