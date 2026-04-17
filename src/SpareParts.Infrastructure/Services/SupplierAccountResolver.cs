using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public sealed class SupplierAccountResolver
    {
        private readonly ISqlConnectionFactory _factory;

        public SupplierAccountResolver(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public int? ResolveAccountId(int? supplierId)
        {
            if (supplierId is not > 0)
            {
                return null;
            }

            using var session = new DbSession(_factory);
            return new SuppliersRepository(session).GetAccountId(supplierId.Value);
        }
    }
}
