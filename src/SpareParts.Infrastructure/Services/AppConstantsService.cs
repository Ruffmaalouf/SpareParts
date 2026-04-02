using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public sealed class AppConstantsService
    {
        private readonly ISqlConnectionFactory _factory;

        public AppConstantsService(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public IEnumerable<AppConstantDto> GetAll()
        {
            using var session = new DbSession(_factory);
            var repository = new AppConstantsRepository(session);
            return repository.GetAll().ToList();
        }
    }
}
