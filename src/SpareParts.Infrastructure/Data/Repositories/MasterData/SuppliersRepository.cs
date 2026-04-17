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

        public IEnumerable<SupplierDto> GetAll()
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Suppliers", "AccountId");
            var sql = hasAccountId
                ? @"SELECT s.Id,
                            s.Name,
                            s.Phone,
                            s.Email,
                            s.Address,
                            s.TaxNumber,
                            s.OpeningBalance,
                            s.AccountId,
                            a.Code AS AccountCode,
                            a.Name AS AccountName
                     FROM Suppliers s
                     LEFT JOIN Accounts a ON a.Id = s.AccountId
                     ORDER BY s.Name;"
                : @"SELECT s.Id,
                            s.Name,
                            s.Phone,
                            s.Email,
                            s.Address,
                            s.TaxNumber,
                            s.OpeningBalance,
                            CAST(NULL AS INT) AS AccountId,
                            CAST(NULL AS NVARCHAR(20)) AS AccountCode,
                            CAST(NULL AS NVARCHAR(160)) AS AccountName
                     FROM Suppliers s
                     ORDER BY s.Name;";

            return _session.Connection.Query<SupplierDto>(sql, transaction: _session.Transaction);
        }

        public int Insert(Supplier supplier)
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Suppliers", "AccountId");
            var sql = hasAccountId
                ? @"INSERT INTO Suppliers
                    (Name, Phone, Email, Address, TaxNumber, OpeningBalance, AccountId, CreatedAt, CreatedByUserId)
                    VALUES
                    (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @AccountId, @CreatedAt, @CreatedByUserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);"
                : @"INSERT INTO Suppliers
                    (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                    VALUES
                    (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return _session.Connection.ExecuteScalar<int>(sql, supplier, _session.Transaction);
        }

        public Supplier? GetById(int id)
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Suppliers", "AccountId");
            var sql = hasAccountId
                ? @"SELECT Id,
                            Name,
                            Phone,
                            Email,
                            Address,
                            TaxNumber,
                            OpeningBalance,
                            AccountId,
                            CreatedAt,
                            CreatedByUserId,
                            ModifiedAt,
                            ModifiedByUserId
                     FROM Suppliers
                     WHERE Id = @Id;"
                : @"SELECT Id,
                            Name,
                            Phone,
                            Email,
                            Address,
                            TaxNumber,
                            OpeningBalance,
                            CAST(NULL AS INT) AS AccountId,
                            CreatedAt,
                            CreatedByUserId,
                            ModifiedAt,
                            ModifiedByUserId
                     FROM Suppliers
                     WHERE Id = @Id;";

            return _session.Connection.QueryFirstOrDefault<Supplier>(sql, new { Id = id }, _session.Transaction);
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

        public void SetAccountId(int id, int? accountId, int userId)
        {
            if (!AccountingSchemaInspector.HasColumn(_session, "dbo.Suppliers", "AccountId"))
            {
                return;
            }

            const string sql = @"UPDATE Suppliers
                                 SET AccountId = @AccountId,
                                     ModifiedAt = @Now,
                                     ModifiedByUserId = @UserId
                                 WHERE Id = @Id;";

            _session.Connection.Execute(sql, new
            {
                Id = id,
                AccountId = accountId,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);
        }

        public int? GetAccountId(int id)
        {
            if (!AccountingSchemaInspector.HasColumn(_session, "dbo.Suppliers", "AccountId"))
            {
                return null;
            }

            const string sql = @"SELECT AccountId
                                 FROM Suppliers
                                 WHERE Id = @Id;";

            return _session.Connection.ExecuteScalar<int?>(sql, new { Id = id }, _session.Transaction);
        }

        public bool UsesAccount(int accountId)
        {
            if (!AccountingSchemaInspector.HasColumn(_session, "dbo.Suppliers", "AccountId"))
            {
                return false;
            }

            const string sql = "SELECT COUNT(1) FROM Suppliers WHERE AccountId = @AccountId;";
            return _session.Connection.ExecuteScalar<int>(sql, new { AccountId = accountId }, _session.Transaction) > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Suppliers WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }
}
