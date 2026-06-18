using Dapper;
using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Data
{
    public sealed class AppConstantsRepository
    {
        private readonly DbSession _session;

        public AppConstantsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<AppConstantDto> GetAll()
        {
            const string sql = @"SELECT [Key], Value
                                 FROM AppConstants
                                 WHERE (@TenantId = 0 OR TenantId = @TenantId)
                                 ORDER BY [Key];";

            return _session.Connection.Query<AppConstantDto>(sql, new { _session.TenantId }, transaction: _session.Transaction);
        }

        public void Upsert(string key, string value, string? description)
        {
            var tenantId = _session.TenantId > 0 ? (int?)_session.TenantId : null;

            const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = @Key AND (@SessionTenantId = 0 OR TenantId = @SessionTenantId))
BEGIN
    UPDATE dbo.AppConstants
    SET [Value] = @Value,
        Description = COALESCE(@Description, Description),
        UpdatedAt = SYSUTCDATETIME()
    WHERE [Key] = @Key AND (@SessionTenantId = 0 OR TenantId = @SessionTenantId);
END
ELSE
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description, TenantId)
    VALUES (@Key, @Value, @Description, @TenantId);
END;";

            _session.Connection.Execute(
                sql,
                new
                {
                    Key = key,
                    Value = value,
                    Description = description,
                    TenantId = tenantId,
                    SessionTenantId = _session.TenantId
                },
                transaction: _session.Transaction);
        }
    }
}
