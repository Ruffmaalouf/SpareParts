using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public sealed class CustomerAccountResolver
    {
        private readonly ISqlConnectionFactory _factory;

        public CustomerAccountResolver(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public int? ResolveAccountId(int? customerId)
        {
            if (customerId is not > 0)
            {
                return null;
            }

            using var session = new DbSession(_factory);
            return new CustomersRepository(session).GetAccountId(customerId.Value);
        }
    }
}
