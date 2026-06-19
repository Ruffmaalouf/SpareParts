using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{
    public sealed class TransactionTypesRepository : ITransactionTypesRepository
    {
        private readonly DbSession _session;

        public TransactionTypesRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<TransactionTypeDto> GetAll()
        {
            const string sql = @"SELECT Id,
                                        TypeKey,
                                        Name,
                                        CurrencyCode,
                                        CounterRate,
                                        SerialNumberFormat,
                                        SerialStartNumber AS StartNumber,
                                        SerialCurrentNumber AS CurrentNumber,
                                        IsActive
                                 FROM TransactionTypes
                                 WHERE (@TenantId = 0 OR TenantId = @TenantId)
                                 ORDER BY SortOrder, Name;";

            return _session.Connection.Query<TransactionTypeDto>(sql, new { _session.TenantId }, transaction: _session.Transaction);
        }

        public TransactionTypeDto? GetById(int id)
        {
            const string sql = @"SELECT Id,
                                        TypeKey,
                                        Name,
                                        CurrencyCode,
                                        CounterRate,
                                        SerialNumberFormat,
                                        SerialStartNumber AS StartNumber,
                                        SerialCurrentNumber AS CurrentNumber,
                                        IsActive
                                 FROM TransactionTypes
                                 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);";

            return _session.Connection.QuerySingleOrDefault<TransactionTypeDto>(
                sql,
                new { Id = id, _session.TenantId },
                _session.Transaction);
        }

        public void Insert(string typeKey, string name, string currencyCode, decimal counterRate, string serialNumberFormat, long startNumber, long currentNumber, bool isActive)
        {
            const string sql = @"INSERT INTO TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder, TenantId)
                                 VALUES (
                                     @TypeKey,
                                     @Name,
                                     @CurrencyCode,
                                     @CounterRate,
                                     @SerialNumberFormat,
                                     @StartNumber,
                                     @CurrentNumber,
                                     @IsActive,
                                     ISNULL((SELECT MAX(SortOrder) + 10 FROM TransactionTypes WHERE (@TenantId = 0 OR TenantId = @TenantId)), 10),
                                     @TenantId);";

            _session.Connection.Execute(sql, new
            {
                TypeKey = typeKey,
                Name = name,
                CurrencyCode = currencyCode,
                CounterRate = counterRate,
                SerialNumberFormat = serialNumberFormat,
                StartNumber = startNumber,
                CurrentNumber = currentNumber,
                IsActive = isActive,
                _session.TenantId
            }, _session.Transaction);
        }

        public bool Update(int id, string typeKey, string name, string currencyCode, decimal counterRate, string serialNumberFormat, long startNumber, long currentNumber, bool isActive)
        {
            const string sql = @"UPDATE TransactionTypes
                                 SET TypeKey = @TypeKey,
                                     Name = @Name,
                                     CurrencyCode = @CurrencyCode,
                                     CounterRate = @CounterRate,
                                     SerialNumberFormat = @SerialNumberFormat,
                                     SerialStartNumber = @StartNumber,
                                     SerialCurrentNumber = @CurrentNumber,
                                     IsActive = @IsActive
                                 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);";

            var affected = _session.Connection.Execute(sql, new
            {
                Id = id,
                TypeKey = typeKey,
                Name = name,
                CurrencyCode = currencyCode,
                CounterRate = counterRate,
                SerialNumberFormat = serialNumberFormat,
                StartNumber = startNumber,
                CurrentNumber = currentNumber,
                IsActive = isActive,
                _session.TenantId
            }, _session.Transaction);

            return affected > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM TransactionTypes WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);";
            return _session.Connection.Execute(sql, new { Id = id, _session.TenantId }, _session.Transaction) > 0;
        }
    }
}
