using Dapper;
using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Data
{
    public interface IBrandsRepository
    {
        IEnumerable<Brand> GetAll();
        int Insert(Brand brand);
        bool Update(int id, string name, bool isActive, int userId);
        bool Delete(int id);
    }

    public class BrandsRepository : IBrandsRepository
    {
        private readonly DbSession _session;

        public BrandsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Brand> GetAll()
        {
            const string sql = "SELECT * FROM Brands ORDER BY Name";
            return _session.Connection.Query<Brand>(sql, transaction: _session.Transaction);
        }

        public int Insert(Brand brand)
        {
            const string sql = @"INSERT INTO Brands (Name, IsActive, CreatedAt, CreatedByUserId)
                                 VALUES (@Name, @IsActive, @CreatedAt, @CreatedByUserId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, brand, _session.Transaction);
        }

        public bool Update(int id, string name, bool isActive, int userId)
        {
            const string sql = @"UPDATE Brands
                                 SET Name = @Name, IsActive = @IsActive,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                Name = name,
                IsActive = isActive,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Brands WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }
}
