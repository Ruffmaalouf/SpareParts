using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public sealed class TransactionTypesService
    {
        private readonly ISqlConnectionFactory _factory;

        public TransactionTypesService(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public IEnumerable<TransactionTypeDto> GetAll()
        {
            using var session = new DbSession(_factory);
            var repository = new TransactionTypesRepository(session);
            return repository.GetAll().ToList();
        }

        public void Create(CreateTransactionTypeRequest request)
        {
            var normalized = Normalize(request);

            using var session = new DbSession(_factory);
            var repository = new TransactionTypesRepository(session);
            repository.Insert(normalized.Name, normalized.CurrencyCode, normalized.CounterRate, normalized.IsActive);
            session.Commit();
        }

        public void Update(int id, CreateTransactionTypeRequest request)
        {
            if (id <= 0)
            {
                throw new ValidationException("Invalid id.");
            }

            var normalized = Normalize(request);

            using var session = new DbSession(_factory);
            var repository = new TransactionTypesRepository(session);
            var updated = repository.Update(id, normalized.Name, normalized.CurrencyCode, normalized.CounterRate, normalized.IsActive);
            if (!updated)
            {
                throw new NotFoundException("Transaction type not found.");
            }

            session.Commit();
        }

        public void Delete(int id)
        {
            using var session = new DbSession(_factory);
            var repository = new TransactionTypesRepository(session);
            var deleted = repository.Delete(id);
            if (!deleted)
            {
                throw new NotFoundException("Transaction type not found.");
            }

            session.Commit();
        }

        private static (string Name, string CurrencyCode, decimal CounterRate, bool IsActive) Normalize(CreateTransactionTypeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ValidationException("Transaction type name is required.");
            }

            var currencyCode = (request.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
            if (currencyCode.Length != 3)
            {
                throw new ValidationException("Currency code must be exactly 3 characters.");
            }

            if (request.CounterRate <= 0)
            {
                throw new ValidationException("Counter rate must be greater than zero.");
            }

            return (request.Name.Trim(), currencyCode, request.CounterRate, request.IsActive);
        }
    }
}
