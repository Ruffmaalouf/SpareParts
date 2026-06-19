using Dapper;
using SpareParts.Domain.MasterData;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class BrandsRepository : IBrandsRepository
    {
        private readonly DbSession _session;

        public BrandsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Brand> GetAll()
        {
            const string sql = "SELECT * FROM Brands WHERE (@TenantId = 0 OR TenantId = @TenantId) ORDER BY Name";
            return _session.Connection.Query<Brand>(sql, new { _session.TenantId }, _session.Transaction);
        }

        public (IEnumerable<Brand> Items, int TotalCount) GetPaged(int page, int pageSize)
        {
            const string sql = @"
SELECT COUNT(1) FROM Brands WHERE (@TenantId = 0 OR TenantId = @TenantId);

SELECT *
FROM Brands
WHERE (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY Name
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = _session.Connection.QueryMultiple(
                sql,
                new { Offset = (page - 1) * pageSize, PageSize = pageSize, _session.TenantId },
                _session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<Brand>().ToList();
            return (items, totalCount);
        }

        public int Insert(Brand brand)
        {
            const string sql = @"INSERT INTO Brands (Name, IsActive, CreatedAt, CreatedByUserId, TenantId)
                                 VALUES (@Name, @IsActive, @CreatedAt, @CreatedByUserId, @TenantId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            return _session.Connection.ExecuteScalar<int>(sql, new
            {
                brand.Name,
                brand.IsActive,
                brand.CreatedAt,
                brand.CreatedByUserId,
                TenantId = tenantId
            }, _session.Transaction);
        }

        public bool Update(int id, string name, bool isActive, int userId)
        {
            const string sql = @"UPDATE Brands
                                 SET Name = @Name, IsActive = @IsActive,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                Name = name,
                IsActive = isActive,
                Now = DateTime.UtcNow,
                UserId = userId,
                _session.TenantId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Brands WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)";
            var deleted = _session.Connection.Execute(sql, new { Id = id, _session.TenantId }, _session.Transaction);
            return deleted > 0;
        }
    }
}
