using Dapper;
using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Data
{
    public sealed class AppConstantsRepository
    {
        private readonly DbSession _session;

        public AppConstantsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<AppConstantDto> GetAll()
        {
            const string sql = @"SELECT [Key], Value
                                 FROM AppConstants
                                 ORDER BY [Key];";

            return _session.Connection.Query<AppConstantDto>(sql, transaction: _session.Transaction);
        }
    }
}
