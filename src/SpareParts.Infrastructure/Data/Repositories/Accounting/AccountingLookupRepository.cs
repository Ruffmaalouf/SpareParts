using Dapper;
using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Data
{
    public sealed class AccountingLookupRepository
    {
        private static readonly AccountTypeDefinitionDto[] DefaultAccountTypes =
        [
            new() { TypeKey = "asset", Label = "Asset", Description = "Resources owned or controlled by the business.", SortOrder = 10 },
            new() { TypeKey = "liability", Label = "Liability", Description = "Amounts owed by the business.", SortOrder = 20 },
            new() { TypeKey = "equity", Label = "Equity", Description = "Owner equity and retained balances.", SortOrder = 30 },
            new() { TypeKey = "income", Label = "Income", Description = "Revenue and income accounts.", SortOrder = 40 },
            new() { TypeKey = "expense", Label = "Expense", Description = "Expense and cost accounts.", SortOrder = 50 }
        ];

        private static readonly PostingRoleDefinitionDto[] DefaultPostingRoles =
        [
            new() { RoleKey = "sales_cash", Label = "Sales Cash", Description = "Debited when a sale has no customer-specific receivable account.", SortOrder = 10, IsActive = true },
            new() { RoleKey = "sales_revenue", Label = "Sales Revenue", Description = "Credited for the revenue side of each sale.", SortOrder = 20, IsActive = true },
            new() { RoleKey = "cogs", Label = "Cost of Goods Sold", Description = "Debited for the inventory cost leaving stock on a sale.", SortOrder = 30, IsActive = true },
            new() { RoleKey = "inventory", Label = "Inventory", Description = "Inventory control account used by both sales and purchase postings.", SortOrder = 40, IsActive = true },
            new() { RoleKey = "purchase_offset", Label = "Purchase Offset", Description = "Credited when a purchase invoice is posted.", SortOrder = 50, IsActive = true }
        ];

        private readonly DbSession _session;

        public AccountingLookupRepository(DbSession session)
        {
            _session = session;
        }

        public IReadOnlyList<AccountTypeDefinitionDto> GetAccountTypes()
        {
            if (!AccountingSchemaInspector.HasTable(_session, "dbo.AccountingAccountTypes"))
            {
                return DefaultAccountTypes.ToList();
            }

            const string sql = @"SELECT TypeKey,
                                        Label,
                                        Description,
                                        SortOrder,
                                        IsActive
                                 FROM AccountingAccountTypes
                                 WHERE IsActive = 1
                                 ORDER BY SortOrder, Label;";

            return _session.Connection.Query<AccountTypeDefinitionDto>(sql, transaction: _session.Transaction).ToList();
        }

        public AccountTypeDefinitionDto? GetAccountType(string typeKey)
        {
            const string sql = @"SELECT TypeKey,
                                        Label,
                                        Description,
                                        SortOrder,
                                        IsActive
                                 FROM AccountingAccountTypes
                                 WHERE TypeKey = @TypeKey;";

            return _session.Connection.QueryFirstOrDefault<AccountTypeDefinitionDto>(sql, new { TypeKey = typeKey }, _session.Transaction);
        }

        public void InsertAccountType(AccountTypeDefinitionDto item)
        {
            const string sql = @"INSERT INTO AccountingAccountTypes
                                    (TypeKey, Label, Description, SortOrder, IsActive)
                                 VALUES
                                    (@TypeKey, @Label, @Description, @SortOrder, 1);";

            _session.Connection.Execute(sql, item, _session.Transaction);
        }

        public bool UpdateAccountType(string typeKey, string label, string description, int sortOrder)
        {
            const string sql = @"UPDATE AccountingAccountTypes
                                 SET Label = @Label,
                                     Description = @Description,
                                     SortOrder = @SortOrder,
                                     IsActive = 1
                                 WHERE TypeKey = @TypeKey;";

            return _session.Connection.Execute(sql, new
            {
                TypeKey = typeKey,
                Label = label,
                Description = description,
                SortOrder = sortOrder
            }, _session.Transaction) > 0;
        }

        public bool DeleteAccountType(string typeKey)
        {
            const string sql = "DELETE FROM AccountingAccountTypes WHERE TypeKey = @TypeKey;";
            return _session.Connection.Execute(sql, new { TypeKey = typeKey }, _session.Transaction) > 0;
        }

        public bool UsesAccountType(string typeKey)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(_session, "dbo.Accounts", "AccountTypeKey");
            var accountTypeExpression = hasAccountTypeKey ? "AccountTypeKey" : "AccountType";
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(accountTypeExpression);
            var sql = $@"SELECT COUNT(1)
                         FROM Accounts
                         WHERE {normalizedAccountTypeKey} = @TypeKey;";

            return _session.Connection.ExecuteScalar<int>(sql, new { TypeKey = typeKey }, _session.Transaction) > 0;
        }

        public IReadOnlyList<PostingRoleDefinitionDto> GetPostingRoles()
        {
            if (!AccountingSchemaInspector.HasTable(_session, "dbo.AccountingPostingRoles"))
            {
                return DefaultPostingRoles.ToList();
            }

            const string sql = @"SELECT RoleKey,
                                        Label,
                                        Description,
                                        SortOrder,
                                        IsActive
                                 FROM AccountingPostingRoles
                                 WHERE IsActive = 1
                                 ORDER BY SortOrder, Label;";

            return _session.Connection.Query<PostingRoleDefinitionDto>(sql, transaction: _session.Transaction).ToList();
        }

        public PostingRoleDefinitionDto? GetPostingRole(string roleKey)
        {
            const string sql = @"SELECT RoleKey,
                                        Label,
                                        Description,
                                        SortOrder,
                                        IsActive
                                 FROM AccountingPostingRoles
                                 WHERE RoleKey = @RoleKey;";

            return _session.Connection.QueryFirstOrDefault<PostingRoleDefinitionDto>(sql, new { RoleKey = roleKey }, _session.Transaction);
        }

        public void InsertPostingRole(PostingRoleDefinitionDto item)
        {
            const string sql = @"INSERT INTO AccountingPostingRoles
                                    (RoleKey, Label, Description, SortOrder, IsActive)
                                 VALUES
                                    (@RoleKey, @Label, @Description, @SortOrder, 1);";

            _session.Connection.Execute(sql, item, _session.Transaction);
        }

        public bool UpdatePostingRole(string roleKey, string label, string description, int sortOrder)
        {
            const string sql = @"UPDATE AccountingPostingRoles
                                 SET Label = @Label,
                                     Description = @Description,
                                     SortOrder = @SortOrder,
                                     IsActive = 1
                                 WHERE RoleKey = @RoleKey;";

            return _session.Connection.Execute(sql, new
            {
                RoleKey = roleKey,
                Label = label,
                Description = description,
                SortOrder = sortOrder
            }, _session.Transaction) > 0;
        }

        public bool DeletePostingRole(string roleKey)
        {
            const string sql = "DELETE FROM AccountingPostingRoles WHERE RoleKey = @RoleKey;";
            return _session.Connection.Execute(sql, new { RoleKey = roleKey }, _session.Transaction) > 0;
        }

        public bool UsesPostingRole(string roleKey)
        {
            if (!AccountingSchemaInspector.HasTable(_session, "dbo.AccountingPostingSettings"))
            {
                return false;
            }

            const string sql = "SELECT COUNT(1) FROM AccountingPostingSettings WHERE SettingKey = @RoleKey;";
            return _session.Connection.ExecuteScalar<int>(sql, new { RoleKey = roleKey }, _session.Transaction) > 0;
        }
    }
}
