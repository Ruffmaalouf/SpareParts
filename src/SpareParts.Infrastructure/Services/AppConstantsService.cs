using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class AppConstantsService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public AppConstantsService(ISqlConnectionFactory factory, ITenantContext tenantContext)
        {
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public IEnumerable<AppConstantDto> GetAll()
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repository = new AppConstantsRepository(session);
            return repository.GetAll().ToList();
        }

        public void Upsert(string key, UpsertAppConstantRequest request)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ValidationException("App constant key is required.");
            }

            if (request == null)
            {
                throw new ValidationException("App constant request is required.");
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repository = new AppConstantsRepository(session);

            repository.Upsert(
                key.Trim(),
                request.Value ?? string.Empty,
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim());

            session.Commit();
        }
    }
}
