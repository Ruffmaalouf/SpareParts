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
                                 ORDER BY [Key];";

            return _session.Connection.Query<AppConstantDto>(sql, transaction: _session.Transaction);
        }

        public void Upsert(string key, string value, string? description)
        {
            const string sql = @"
IF EXISTS (SELECT 1 FROM dbo.AppConstants WHERE [Key] = @Key)
BEGIN
    UPDATE dbo.AppConstants
    SET [Value] = @Value,
        Description = COALESCE(@Description, Description),
        UpdatedAt = SYSUTCDATETIME()
    WHERE [Key] = @Key;
END
ELSE
BEGIN
    INSERT INTO dbo.AppConstants ([Key], [Value], Description)
    VALUES (@Key, @Value, @Description);
END;";

            _session.Connection.Execute(
                sql,
                new
                {
                    Key = key,
                    Value = value,
                    Description = description
                },
                transaction: _session.Transaction);
        }
    }
}
