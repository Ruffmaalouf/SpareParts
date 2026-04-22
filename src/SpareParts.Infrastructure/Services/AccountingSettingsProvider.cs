using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public sealed class AccountingSettingsProvider
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingOptions _options;

        public AccountingSettingsProvider(ISqlConnectionFactory factory, AccountingOptions options)
        {
            _factory = factory;
            _options = options;
        }

        public AccountingPostingSettingsSnapshot GetSnapshot()
        {
            using var conn = _factory.CreateConnection();
            var rows = conn.Query<AccountingPostingSettingRecord>(
                @"SELECT SettingKey, AccountId
                  FROM AccountingPostingSettings;")
                .ToDictionary(item => item.SettingKey, item => item.AccountId, StringComparer.OrdinalIgnoreCase);

            var defaults = _options;

            return new AccountingPostingSettingsSnapshot
            {
                SalesCashAccountId = Resolve(rows, AccountingSettingKeys.SalesCash, defaults.CashAccountId),
                SalesRevenueAccountId = Resolve(rows, AccountingSettingKeys.SalesRevenue, defaults.SalesAccountId),
                CogsAccountId = Resolve(rows, AccountingSettingKeys.Cogs, defaults.CogsAccountId),
                InventoryAccountId = Resolve(rows, AccountingSettingKeys.Inventory, defaults.InventoryAccountId),
                PurchaseOffsetAccountId = Resolve(rows, AccountingSettingKeys.PurchaseOffset, defaults.CashOrApAccountId)
            };
        }

        private static int Resolve(IDictionary<string, int> rows, string key, int fallback)
            => rows.TryGetValue(key, out var accountId) && accountId > 0
                ? accountId
                : fallback;
    }
}
