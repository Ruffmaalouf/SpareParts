using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    public sealed class AccountsRepository : IAccountsRepository
    {
        private readonly DbSession _session;

        public AccountsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<AccountDto> GetAll()
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var hasAccountTypeLookup = AccountingSchemaInspector.HasTable(_session, "dbo.AccountingAccountTypes");
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var accountTypeSource = hasAccountTypeKey ? "a.AccountTypeKey" : "a.AccountType";
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(accountTypeSource);
            var accountTypeLabel = hasAccountTypeLookup
                ? $"ISNULL(t.Label, {AccountingSql.AccountTypeLabel(normalizedAccountTypeKey)})"
                : AccountingSql.AccountTypeLabel(normalizedAccountTypeKey);
            var tenantFilter = hasTenantId ? "WHERE (@TenantId = 0 OR a.TenantId = @TenantId)" : string.Empty;
            var sql = $@"SELECT a.Id,
                                a.Code,
                                a.Name,
                                {normalizedAccountTypeKey} AS AccountTypeKey,
                                {accountTypeLabel} AS AccountType,
                                a.ParentId,
                                p.Code AS ParentCode,
                                p.Name AS ParentName
                         FROM Accounts a
                         {(hasAccountTypeLookup ? $"LEFT JOIN AccountingAccountTypes t ON t.TypeKey = {normalizedAccountTypeKey}" : string.Empty)}
                         LEFT JOIN Accounts p ON p.Id = a.ParentId
                         {tenantFilter}
                         ORDER BY a.Code, a.Name;";

            return _session.Connection.Query<AccountDto>(
                sql,
                new { TenantId = _session.TenantId },
                _session.Transaction);
        }

        public Account? GetById(int id)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(hasAccountTypeKey ? "AccountTypeKey" : "AccountType");
            var tenantFilter = hasTenantId ? "AND (@TenantId = 0 OR TenantId = @TenantId)" : string.Empty;
            var sql = $@"SELECT Id,
                                Code,
                                Name,
                                {normalizedAccountTypeKey} AS AccountTypeKey,
                                ParentId,
                                CreatedAt,
                                CreatedByUserId,
                                ModifiedAt,
                                ModifiedByUserId
                         FROM Accounts
                         WHERE Id = @Id {tenantFilter};";

            return _session.Connection.QueryFirstOrDefault<Account>(
                sql,
                new { Id = id, TenantId = _session.TenantId },
                _session.Transaction);
        }

        public Account? GetByCode(string code)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(hasAccountTypeKey ? "AccountTypeKey" : "AccountType");
            var tenantFilter = hasTenantId ? "AND (@TenantId = 0 OR TenantId = @TenantId)" : string.Empty;
            var sql = $@"SELECT Id,
                                Code,
                                Name,
                                {normalizedAccountTypeKey} AS AccountTypeKey,
                                ParentId,
                                CreatedAt,
                                CreatedByUserId,
                                ModifiedAt,
                                ModifiedByUserId
                         FROM Accounts
                         WHERE Code = @Code {tenantFilter};";

            return _session.Connection.QueryFirstOrDefault<Account>(
                sql,
                new { Code = code, TenantId = _session.TenantId },
                _session.Transaction);
        }

        public int Insert(Account account)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;

            if (hasAccountTypeKey)
            {
                var sql = hasTenantId
                    ? @"INSERT INTO Accounts
                        (Code, Name, AccountType, AccountTypeKey, ParentId, TenantId, CreatedAt, CreatedByUserId)
                        VALUES
                        (@Code, @Name, @AccountType, @AccountTypeKey, @ParentId, @TenantId, @CreatedAt, @CreatedByUserId);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);"
                    : @"INSERT INTO Accounts
                        (Code, Name, AccountType, AccountTypeKey, ParentId, CreatedAt, CreatedByUserId)
                        VALUES
                        (@Code, @Name, @AccountType, @AccountTypeKey, @ParentId, @CreatedAt, @CreatedByUserId);
                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                return _session.Connection.ExecuteScalar<int>(sql, new
                {
                    account.Code,
                    account.Name,
                    AccountType = AccountingSql.ToLegacyAccountType(account.AccountTypeKey),
                    account.AccountTypeKey,
                    account.ParentId,
                    TenantId = tenantId,
                    account.CreatedAt,
                    account.CreatedByUserId
                }, _session.Transaction);
            }

            var legacySql = hasTenantId
                ? @"INSERT INTO Accounts
                    (Code, Name, AccountType, ParentId, TenantId, CreatedAt, CreatedByUserId)
                    VALUES
                    (@Code, @Name, @AccountType, @ParentId, @TenantId, @CreatedAt, @CreatedByUserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);"
                : @"INSERT INTO Accounts
                    (Code, Name, AccountType, ParentId, CreatedAt, CreatedByUserId)
                    VALUES
                    (@Code, @Name, @AccountType, @ParentId, @CreatedAt, @CreatedByUserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return _session.Connection.ExecuteScalar<int>(legacySql, new
            {
                account.Code,
                account.Name,
                AccountType = AccountingSql.ToLegacyAccountType(account.AccountTypeKey),
                account.ParentId,
                TenantId = tenantId,
                account.CreatedAt,
                account.CreatedByUserId
            }, _session.Transaction);
        }

        public bool Update(int id, string code, string name, string accountTypeKey, int? parentId, int userId)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var tenantFilter = hasTenantId ? "AND (@TenantId = 0 OR TenantId = @TenantId)" : string.Empty;
            var sql = hasAccountTypeKey
                ? $@"UPDATE Accounts
                    SET Code = @Code,
                        Name = @Name,
                        AccountType = @AccountType,
                        AccountTypeKey = @AccountTypeKey,
                        ParentId = @ParentId,
                        ModifiedAt = @ModifiedAt,
                        ModifiedByUserId = @ModifiedByUserId
                    WHERE Id = @Id {tenantFilter};"
                : $@"UPDATE Accounts
                    SET Code = @Code,
                        Name = @Name,
                        AccountType = @AccountType,
                        ParentId = @ParentId,
                        ModifiedAt = @ModifiedAt,
                        ModifiedByUserId = @ModifiedByUserId
                    WHERE Id = @Id {tenantFilter};";

            var affected = _session.Connection.Execute(sql, new
            {
                Id = id,
                Code = code,
                Name = name,
                AccountType = AccountingSql.ToLegacyAccountType(accountTypeKey),
                AccountTypeKey = accountTypeKey,
                ParentId = parentId,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId,
                TenantId = _session.TenantId
            }, _session.Transaction);

            return affected > 0;
        }

        public bool Delete(int id)
        {
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var tenantFilter = hasTenantId ? "AND (@TenantId = 0 OR TenantId = @TenantId)" : string.Empty;
            var sql = $"DELETE FROM Accounts WHERE Id = @Id {tenantFilter};";
            return _session.Connection.Execute(
                sql,
                new { Id = id, TenantId = _session.TenantId },
                _session.Transaction) > 0;
        }

        public bool HasChildren(int id)
        {
            var hasTenantId = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "TenantId");
            var tenantFilter = hasTenantId ? "AND (@TenantId = 0 OR TenantId = @TenantId)" : string.Empty;
            var sql = $"SELECT COUNT(1) FROM Accounts WHERE ParentId = @Id {tenantFilter};";
            return _session.Connection.ExecuteScalar<int>(
                sql,
                new { Id = id, TenantId = _session.TenantId },
                _session.Transaction) > 0;
        }
    }
}
