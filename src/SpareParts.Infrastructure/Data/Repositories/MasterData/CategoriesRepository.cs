using Dapper;
using SpareParts.Domain.MasterData;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class CategoriesRepository : ICategoriesRepository
    {
        private readonly DbSession _session;

        public CategoriesRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Category> GetAll()
        {
            const string sql = "SELECT * FROM Categories WHERE (@TenantId = 0 OR TenantId = @TenantId) ORDER BY Name";
            return _session.Connection.Query<Category>(sql, new { _session.TenantId }, _session.Transaction);
        }

        public int Insert(Category category)
        {
            const string sql = @"INSERT INTO Categories (Name, ParentId, CreatedAt, CreatedByUserId, TenantId)
                                 VALUES (@Name, @ParentId, @CreatedAt, @CreatedByUserId, @TenantId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            return _session.Connection.ExecuteScalar<int>(sql, new
            {
                category.Name,
                category.ParentId,
                category.CreatedAt,
                category.CreatedByUserId,
                TenantId = tenantId
            }, _session.Transaction);
        }
    }
}
