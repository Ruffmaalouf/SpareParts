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
                                 ORDER BY SettingKey;";

            return _session.Connection.Query<AccountingPostingSettingRecord>(sql, transaction: _session.Transaction);
        }

        public void Upsert(string settingKey, int accountId, int? userId)
        {
            const string sql = @"MERGE dbo.AccountingPostingSettings AS target
                                 USING (SELECT @SettingKey AS SettingKey, @AccountId AS AccountId) AS source
                                 ON target.SettingKey = source.SettingKey
                                 WHEN MATCHED THEN
                                     UPDATE SET
                                         AccountId = source.AccountId,
                                         ModifiedAt = SYSUTCDATETIME(),
                                         ModifiedByUserId = @ModifiedByUserId
                                 WHEN NOT MATCHED THEN
                                     INSERT (SettingKey, AccountId, ModifiedAt, ModifiedByUserId)
                                     VALUES (source.SettingKey, source.AccountId, SYSUTCDATETIME(), @ModifiedByUserId);";

            _session.Connection.Execute(sql, new
            {
                SettingKey = settingKey,
                AccountId = accountId,
                ModifiedByUserId = userId
            }, _session.Transaction);
        }

        public void Delete(string settingKey)
        {
            const string sql = "DELETE FROM AccountingPostingSettings WHERE SettingKey = @SettingKey;";
            _session.Connection.Execute(sql, new { SettingKey = settingKey }, _session.Transaction);
        }

        public bool UsesAccount(int accountId)
        {
            const string sql = "SELECT COUNT(1) FROM AccountingPostingSettings WHERE AccountId = @AccountId;";
            return _session.Connection.ExecuteScalar<int>(sql, new { AccountId = accountId }, _session.Transaction) > 0;
        }
    }

    public sealed class AccountingPostingSettingRecord
    {
        public string SettingKey { get; set; } = string.Empty;
        public int AccountId { get; set; }
    }
}
