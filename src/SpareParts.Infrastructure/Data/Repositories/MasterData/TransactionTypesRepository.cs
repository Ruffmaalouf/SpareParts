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
            const string sql = @"SELECT Id, Name, CurrencyCode, CounterRate, IsActive
                                 FROM TransactionTypes
                                 ORDER BY Name;";

            return _session.Connection.Query<TransactionTypeDto>(sql, transaction: _session.Transaction);
        }

        public void Insert(string name, string currencyCode, decimal counterRate, bool isActive)
        {
            const string sql = @"INSERT INTO TransactionTypes (Name, CurrencyCode, CounterRate, IsActive)
                                 VALUES (@Name, @CurrencyCode, @CounterRate, @IsActive);";

            _session.Connection.Execute(sql, new
            {
                Name = name,
                CurrencyCode = currencyCode,
                CounterRate = counterRate,
                IsActive = isActive
            }, _session.Transaction);
        }

        public bool Update(int id, string name, string currencyCode, decimal counterRate, bool isActive)
        {
            const string sql = @"UPDATE TransactionTypes
                                 SET Name = @Name,
                                     CurrencyCode = @CurrencyCode,
                                     CounterRate = @CounterRate,
                                     IsActive = @IsActive
                                 WHERE Id = @Id;";

            var affected = _session.Connection.Execute(sql, new
            {
                Id = id,
                Name = name,
                CurrencyCode = currencyCode,
                CounterRate = counterRate,
                IsActive = isActive
            }, _session.Transaction);

            return affected > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM TransactionTypes WHERE Id = @Id;";
            return _session.Connection.Execute(sql, new { Id = id }, _session.Transaction) > 0;
        }
    }
}
