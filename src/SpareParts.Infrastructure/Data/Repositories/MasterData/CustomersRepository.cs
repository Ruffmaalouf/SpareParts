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

        public IEnumerable<CustomerDto> GetAll()
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId");
            var tenantFilter = "AND (@TenantId = 0 OR c.TenantId = @TenantId)";
            var sql = hasAccountId
                ? $@"SELECT c.Id,
                            c.Name,
                            c.Phone,
                            c.Email,
                            c.Address,
                            c.TaxNumber,
                            c.OpeningBalance,
                            c.AccountId,
                            a.Code AS AccountCode,
                            a.Name AS AccountName
                     FROM Customers c
                     LEFT JOIN Accounts a ON a.Id = c.AccountId
                     WHERE 1=1 {tenantFilter}
                     ORDER BY c.Name;"
                : $@"SELECT c.Id,
                            c.Name,
                            c.Phone,
                            c.Email,
                            c.Address,
                            c.TaxNumber,
                            c.OpeningBalance,
                            CAST(NULL AS INT) AS AccountId,
                            CAST(NULL AS NVARCHAR(20)) AS AccountCode,
                            CAST(NULL AS NVARCHAR(160)) AS AccountName
                     FROM Customers c
                     WHERE 1=1 {tenantFilter}
                     ORDER BY c.Name;";

            return _session.Connection.Query<CustomerDto>(sql, new { TenantId = _session.TenantId }, transaction: _session.Transaction);
        }

        public (IEnumerable<CustomerDto> Items, int TotalCount) GetPaged(string? search, int page, int pageSize)
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId");
            var searchParam = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";
            var tenantFilter = "AND (@TenantId = 0 OR c.TenantId = @TenantId)";
            var tenantFilterSimple = "AND (@TenantId = 0 OR TenantId = @TenantId)";

            var sql = hasAccountId
                ? $@"SELECT COUNT(1) FROM Customers c WHERE (@Search IS NULL OR Name LIKE @Search) {tenantFilterSimple};

                    SELECT c.Id,
                           c.Name,
                           c.Phone,
                           c.Email,
                           c.Address,
                           c.TaxNumber,
                           c.OpeningBalance,
                           c.AccountId,
                           a.Code AS AccountCode,
                           a.Name AS AccountName
                    FROM Customers c
                    LEFT JOIN Accounts a ON a.Id = c.AccountId
                    WHERE (@Search IS NULL OR c.Name LIKE @Search) {tenantFilter}
                    ORDER BY c.Name
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;"
                : $@"SELECT COUNT(1) FROM Customers c WHERE (@Search IS NULL OR Name LIKE @Search) {tenantFilterSimple};

                    SELECT c.Id,
                           c.Name,
                           c.Phone,
                           c.Email,
                           c.Address,
                           c.TaxNumber,
                           c.OpeningBalance,
                           CAST(NULL AS INT) AS AccountId,
                           CAST(NULL AS NVARCHAR(20)) AS AccountCode,
                           CAST(NULL AS NVARCHAR(160)) AS AccountName
                    FROM Customers c
                    WHERE (@Search IS NULL OR c.Name LIKE @Search) {tenantFilter}
                    ORDER BY c.Name
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            using var multi = _session.Connection.QueryMultiple(
                sql,
                new { Search = searchParam, Offset = (page - 1) * pageSize, PageSize = pageSize, TenantId = _session.TenantId },
                _session.Transaction);

            var totalCount = multi.ReadFirst<int>();
            var items = multi.Read<CustomerDto>().ToList();
            return (items, totalCount);
        }

        public int Insert(Customer customer)
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId");
            var sql = hasAccountId
                ? @"INSERT INTO Customers
                    (Name, Phone, Email, Address, TaxNumber, OpeningBalance, AccountId, TenantId, CreatedAt, CreatedByUserId)
                    VALUES
                    (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @AccountId, @TenantId, @CreatedAt, @CreatedByUserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);"
                : @"INSERT INTO Customers
                    (Name, Phone, Email, Address, TaxNumber, OpeningBalance, TenantId, CreatedAt, CreatedByUserId)
                    VALUES
                    (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @TenantId, @CreatedAt, @CreatedByUserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var parameters = new DynamicParameters(customer);
            parameters.Add("TenantId", _session.TenantId > 0 ? (int?)_session.TenantId : null);
            return _session.Connection.ExecuteScalar<int>(sql, parameters, _session.Transaction);
        }

        public Customer? GetById(int id)
        {
            var hasAccountId = AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId");
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
                     FROM Customers
                     WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);"
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
                     FROM Customers
                     WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);";

            return _session.Connection.QueryFirstOrDefault<Customer>(
                sql,
                new { Id = id, TenantId = _session.TenantId },
                _session.Transaction);
        }

        public bool Update(int id, CreateCustomerRequest request, int userId)
        {
            const string sql = @"UPDATE Customers
                                 SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                                     TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)";
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
                UserId = userId,
                TenantId = _session.TenantId
            }, _session.Transaction);

            return updated > 0;
        }

        public void SetAccountId(int id, int? accountId, int userId)
        {
            if (!AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId"))
            {
                return;
            }

            const string sql = @"UPDATE Customers
                                 SET AccountId = @AccountId,
                                     ModifiedAt = @Now,
                                     ModifiedByUserId = @UserId
                                 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);";

            _session.Connection.Execute(sql, new
            {
                Id = id,
                AccountId = accountId,
                Now = DateTime.UtcNow,
                UserId = userId,
                TenantId = _session.TenantId
            }, _session.Transaction);
        }

        public int? GetAccountId(int id)
        {
            if (!AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId"))
            {
                return null;
            }

            const string sql = @"SELECT AccountId
                                 FROM Customers
                                 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);";

            return _session.Connection.ExecuteScalar<int?>(
                sql,
                new { Id = id, TenantId = _session.TenantId },
                _session.Transaction);
        }

        public bool UsesAccount(int accountId)
        {
            if (!AccountingSchemaInspector.HasColumn(_session, "dbo.Customers", "AccountId"))
            {
                return false;
            }

            const string sql = "SELECT COUNT(1) FROM Customers WHERE AccountId = @AccountId AND (@TenantId = 0 OR TenantId = @TenantId);";
            return _session.Connection.ExecuteScalar<int>(
                sql,
                new { AccountId = accountId, TenantId = _session.TenantId },
                _session.Transaction) > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Customers WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)";
            var deleted = _session.Connection.Execute(
                sql,
                new { Id = id, TenantId = _session.TenantId },
                _session.Transaction);
            return deleted > 0;
        }
    }
}
