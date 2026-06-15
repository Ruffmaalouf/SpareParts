using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class CustomerAccountResolver
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public CustomerAccountResolver(ISqlConnectionFactory factory, ITenantContext? tenantContext = null)
        {
            _factory = factory;
            _tenantContext = tenantContext ?? TenantContext.Legacy;
        }

        public int? ResolveAccountId(int? customerId)
        {
            if (customerId is not > 0)
            {
                return null;
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            return new CustomersRepository(session).GetAccountId(customerId.Value);
        }
    }
}
