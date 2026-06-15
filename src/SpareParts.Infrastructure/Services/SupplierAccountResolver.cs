using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class SupplierAccountResolver
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public SupplierAccountResolver(ISqlConnectionFactory factory, ITenantContext? tenantContext = null)
        {
            _factory = factory;
            _tenantContext = tenantContext ?? TenantContext.Legacy;
        }

        public int? ResolveAccountId(int? supplierId)
        {
            if (supplierId is not > 0)
            {
                return null;
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            return new SuppliersRepository(session).GetAccountId(supplierId.Value);
        }
    }
}
