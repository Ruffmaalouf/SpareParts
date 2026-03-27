namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class SalesRepositories
    {
        internal SalesRepositories(DbSession session) => Invoices = new SalesRepository(session);
        public ISalesRepository Invoices { get; }
    }
}
