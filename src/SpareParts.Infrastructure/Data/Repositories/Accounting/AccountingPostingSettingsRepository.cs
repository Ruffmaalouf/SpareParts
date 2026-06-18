using Dapper;

namespace SpareParts.Infrastructure.Data
{
    public sealed class AccountingPostingSettingsRepository
    {
        private readonly DbSession _session;

        public AccountingPostingSettingsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<AccountingPostingSettingRecord> GetAll()
        {
            const string sql = @"SELECT SettingKey, AccountId
                                 FROM AccountingPostingSettings
                                 WHERE (@TenantId = 0 OR TenantId = @TenantId)
                                 ORDER BY SettingKey;";

            return _session.Connection.Query<AccountingPostingSettingRecord>(sql, new { _session.TenantId }, _session.Transaction);
        }

        public void Upsert(string settingKey, int accountId, int? userId)
        {
            const string sql = @"MERGE dbo.AccountingPostingSettings AS target
                                 USING (SELECT @SettingKey AS SettingKey, @AccountId AS AccountId) AS source
                                 ON target.SettingKey = source.SettingKey
                                    AND (@TenantId = 0 OR target.TenantId = @TenantId)
                                 WHEN MATCHED THEN
                                     UPDATE SET
                                         AccountId = source.AccountId,
                                         ModifiedAt = SYSUTCDATETIME(),
                                         ModifiedByUserId = @ModifiedByUserId
                                 WHEN NOT MATCHED THEN
                                     INSERT (SettingKey, AccountId, ModifiedAt, ModifiedByUserId, TenantId)
                                     VALUES (source.SettingKey, source.AccountId, SYSUTCDATETIME(), @ModifiedByUserId, @InsertTenantId);";

            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;
            _session.Connection.Execute(sql, new
            {
                SettingKey = settingKey,
                AccountId = accountId,
                ModifiedByUserId = userId,
                _session.TenantId,
                InsertTenantId = tenantId
            }, _session.Transaction);
        }

        public void Delete(string settingKey)
        {
            const string sql = "DELETE FROM AccountingPostingSettings WHERE SettingKey = @SettingKey AND (@TenantId = 0 OR TenantId = @TenantId);";
            _session.Connection.Execute(sql, new { SettingKey = settingKey, _session.TenantId }, _session.Transaction);
        }

        public bool UsesAccount(int accountId)
        {
            const string sql = "SELECT COUNT(1) FROM AccountingPostingSettings WHERE AccountId = @AccountId AND (@TenantId = 0 OR TenantId = @TenantId);";
            return _session.Connection.ExecuteScalar<int>(sql, new { AccountId = accountId, _session.TenantId }, _session.Transaction) > 0;
        }
    }
}
