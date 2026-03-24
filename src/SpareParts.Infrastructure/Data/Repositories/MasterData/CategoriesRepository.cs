using Dapper;
using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Data
{
    public interface ICategoriesRepository
    {
        IEnumerable<Category> GetAll();
        int Insert(Category category);
    }

    public class CategoriesRepository : ICategoriesRepository
    {
        private readonly DbSession _session;

        public CategoriesRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Category> GetAll()
        {
            const string sql = "SELECT * FROM Categories ORDER BY Name";
            return _session.Connection.Query<Category>(sql, transaction: _session.Transaction);
        }

        public int Insert(Category category)
        {
            const string sql = @"INSERT INTO Categories (Name, ParentId, CreatedAt, CreatedByUserId)
                                 VALUES (@Name, @ParentId, @CreatedAt, @CreatedByUserId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, category, _session.Transaction);
        }
    }
}
