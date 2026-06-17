using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data.Repositories
{
    public sealed class SalesRepositories
    {
        internal SalesRepositories(DbSession session)
        {
            Invoices = new SalesRepository(session);
            Returns = new SalesReturnRepository(session);
        }

        public ISalesRepository Invoices { get; }
        public ISalesReturnRepository Returns { get; }
    }
}
