using Dapper;
using SpareParts.Domain.BusinessPartners;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class CustomersRepository : ICustomersRepository
    {
        private readonly DbSession _session;

        public CustomersRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Customer> GetAll()
        {
            const string sql = "SELECT * FROM Customers ORDER BY Name";
            return _session.Connection.Query<Customer>(sql, transaction: _session.Transaction);
        }

        public int Insert(Customer customer)
        {
            const string sql = @"INSERT INTO Customers
                (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, customer, _session.Transaction);
        }

        public bool Update(int id, CreateCustomerRequest request, int userId)
        {
            const string sql = @"UPDATE Customers
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
            const string sql = "DELETE FROM Customers WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }
}
