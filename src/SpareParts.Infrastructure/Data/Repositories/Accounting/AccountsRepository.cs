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
            var accountTypeSource = hasAccountTypeKey ? "a.AccountTypeKey" : "a.AccountType";
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(accountTypeSource);
            var accountTypeLabel = hasAccountTypeLookup
                ? $"ISNULL(t.Label, {AccountingSql.AccountTypeLabel(normalizedAccountTypeKey)})"
                : AccountingSql.AccountTypeLabel(normalizedAccountTypeKey);
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
                         ORDER BY a.Code, a.Name;";

            return _session.Connection.Query<AccountDto>(sql, transaction: _session.Transaction);
        }

        public Account? GetById(int id)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(hasAccountTypeKey ? "AccountTypeKey" : "AccountType");
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
                         WHERE Id = @Id;";

            return _session.Connection.QueryFirstOrDefault<Account>(sql, new { Id = id }, _session.Transaction);
        }

        public Account? GetByCode(string code)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(hasAccountTypeKey ? "AccountTypeKey" : "AccountType");
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
                         WHERE Code = @Code;";

            return _session.Connection.QueryFirstOrDefault<Account>(sql, new { Code = code }, _session.Transaction);
        }

        public int Insert(Account account)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");

            if (hasAccountTypeKey)
            {
                const string sql = @"INSERT INTO Accounts
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
                    account.CreatedAt,
                    account.CreatedByUserId
                }, _session.Transaction);
            }

            const string legacySql = @"INSERT INTO Accounts
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
                account.CreatedAt,
                account.CreatedByUserId
            }, _session.Transaction);
        }

        public bool Update(int id, string code, string name, string accountTypeKey, int? parentId, int userId)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var sql = hasAccountTypeKey
                ? @"UPDATE Accounts
                    SET Code = @Code,
                        Name = @Name,
                        AccountType = @AccountType,
                        AccountTypeKey = @AccountTypeKey,
                        ParentId = @ParentId,
                        ModifiedAt = @ModifiedAt,
                        ModifiedByUserId = @ModifiedByUserId
                    WHERE Id = @Id;"
                : @"UPDATE Accounts
                    SET Code = @Code,
                        Name = @Name,
                        AccountType = @AccountType,
                        ParentId = @ParentId,
                        ModifiedAt = @ModifiedAt,
                        ModifiedByUserId = @ModifiedByUserId
                    WHERE Id = @Id;";

            var affected = _session.Connection.Execute(sql, new
            {
                Id = id,
                Code = code,
                Name = name,
                AccountType = AccountingSql.ToLegacyAccountType(accountTypeKey),
                AccountTypeKey = accountTypeKey,
                ParentId = parentId,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId
            }, _session.Transaction);

            return affected > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Accounts WHERE Id = @Id;";
            return _session.Connection.Execute(sql, new { Id = id }, _session.Transaction) > 0;
        }

        public bool HasChildren(int id)
        {
            const string sql = "SELECT COUNT(1) FROM Accounts WHERE ParentId = @Id;";
            return _session.Connection.ExecuteScalar<int>(sql, new { Id = id }, _session.Transaction) > 0;
        }
    }
}
