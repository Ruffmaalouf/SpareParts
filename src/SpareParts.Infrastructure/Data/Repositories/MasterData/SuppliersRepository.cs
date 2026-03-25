using Dapper;
using SpareParts.Domain.BusinessPartners;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class SuppliersRepository : ISuppliersRepository
    {
        private readonly DbSession _session;

        public SuppliersRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Supplier> GetAll()
        {
            const string sql = "SELECT * FROM Suppliers ORDER BY Name";
            return _session.Connection.Query<Supplier>(sql, transaction: _session.Transaction);
        }

        public int Insert(Supplier supplier)
        {
            const string sql = @"INSERT INTO Suppliers
                (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, supplier, _session.Transaction);
        }

        public bool Update(int id, CreateSupplierRequest request, int userId)
        {
            const string sql = @"UPDATE Suppliers
                                 SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                                     TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                request.Name,
                request.Phone,
                request.Email,
                request.Address,
                request.TaxNumber,
                request.OpeningBalance,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Suppliers WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }
}
