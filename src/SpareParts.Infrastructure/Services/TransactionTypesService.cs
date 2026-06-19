using SpareParts.Domain.MasterData;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class TransactionTypesService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public TransactionTypesService(ISqlConnectionFactory factory, ITenantContext tenantContext)
        {
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public IEnumerable<TransactionTypeDto> GetAll()
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repository = new TransactionTypesRepository(session);
            return repository.GetAll().ToList();
        }

        public void Create(CreateTransactionTypeRequest request)
        {
            var normalized = Normalize(request);

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repository = new TransactionTypesRepository(session);
            var existingTypes = repository.GetAll().ToList();
            var typeKey = BuildUniqueTypeKey(normalized.Name, existingTypes.Select(item => item.TypeKey));
            var serialNumberFormat = NormalizeSerialNumberFormat(normalized.SerialNumberFormat, typeKey);

            repository.Insert(
                typeKey,
                normalized.Name,
                normalized.CurrencyCode,
                normalized.CounterRate,
                serialNumberFormat,
                normalized.StartNumber,
                normalized.CurrentNumber,
                normalized.IsActive);
            session.Commit();
        }

        public void Update(int id, CreateTransactionTypeRequest request)
        {
            if (id <= 0)
            {
                throw new ValidationException("Invalid id.");
            }

            var normalized = Normalize(request);

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repository = new TransactionTypesRepository(session);
            var existing = repository.GetById(id);
            if (existing == null)
            {
                throw new NotFoundException("Transaction type not found.");
            }

            var typeKey = string.IsNullOrWhiteSpace(existing.TypeKey)
                ? BuildUniqueTypeKey(normalized.Name, repository.GetAll()
                    .Where(item => item.Id != id)
                    .Select(item => item.TypeKey))
                : existing.TypeKey;
            var serialNumberFormat = NormalizeSerialNumberFormat(normalized.SerialNumberFormat, typeKey);

            var updated = repository.Update(
                id,
                typeKey,
                normalized.Name,
                normalized.CurrencyCode,
                normalized.CounterRate,
                serialNumberFormat,
                normalized.StartNumber,
                normalized.CurrentNumber,
                normalized.IsActive);
            if (!updated)
            {
                throw new NotFoundException("Transaction type not found.");
            }

            session.Commit();
        }

        public void Delete(int id)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repository = new TransactionTypesRepository(session);
            var existing = repository.GetById(id);
            if (existing == null)
            {
                throw new NotFoundException("Transaction type not found.");
            }

            if (string.Equals(existing.TypeKey, TransactionTypeKeys.Sale, StringComparison.OrdinalIgnoreCase)
                || string.Equals(existing.TypeKey, TransactionTypeKeys.Purchase, StringComparison.OrdinalIgnoreCase)
                || string.Equals(existing.TypeKey, TransactionTypeKeys.UsedCarPurchase, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("Core transaction types cannot be deleted.");
            }

            var deleted = repository.Delete(id);
            if (!deleted)
            {
                throw new NotFoundException("Transaction type not found.");
            }

            session.Commit();
        }

        private static (string Name, string CurrencyCode, decimal CounterRate, string SerialNumberFormat, long StartNumber, long CurrentNumber, bool IsActive) Normalize(CreateTransactionTypeRequest request)
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

            if (request.StartNumber <= 0)
            {
                throw new ValidationException("Start number must be greater than zero.");
            }

            if (request.CurrentNumber < 0)
            {
                throw new ValidationException("Current number cannot be negative.");
            }

            return (
                request.Name.Trim(),
                currencyCode,
                request.CounterRate,
                request.SerialNumberFormat,
                request.StartNumber,
                request.CurrentNumber,
                request.IsActive);
        }

        private static string NormalizeSerialNumberFormat(string rawFormat, string typeKey)
        {
            var format = string.IsNullOrWhiteSpace(rawFormat)
                ? BuildDefaultSerialNumberFormat(typeKey)
                : rawFormat.Trim();

            TransactionSerialNumberFormatter.Validate(format);
            return format;
        }

        private static string BuildDefaultSerialNumberFormat(string typeKey)
            => typeKey switch
            {
                TransactionTypeKeys.Sale => "INV-{DATE:yyyyMMdd}-{NUMBER:00000000}",
                TransactionTypeKeys.Purchase => "PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}",
                TransactionTypeKeys.UsedCarPurchase => "PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}",
                _ => "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}"
            };

        private static string BuildUniqueTypeKey(string name, IEnumerable<string> existingTypeKeys)
        {
            var normalized = NormalizeLookupLikeKey(name);
            var reserved = new HashSet<string>(
                existingTypeKeys
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Select(item => item.Trim()),
                StringComparer.OrdinalIgnoreCase);

            if (!reserved.Contains(normalized))
            {
                return normalized;
            }

            for (var suffix = 2; suffix < 1000; suffix++)
            {
                var candidate = $"{normalized}_{suffix}";
                if (!reserved.Contains(candidate))
                {
                    return candidate;
                }
            }

            throw new ValidationException("Could not generate a unique transaction type key.");
        }

        private static string NormalizeLookupLikeKey(string rawValue)
        {
            var input = (rawValue ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ValidationException("Transaction type name is required.");
            }

            var builder = new System.Text.StringBuilder(input.Length);
            var lastWasSeparator = false;

            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasSeparator = false;
                    continue;
                }

                if ((ch == '_' || ch == '-' || char.IsWhiteSpace(ch)) && builder.Length > 0 && !lastWasSeparator)
                {
                    builder.Append('_');
                    lastWasSeparator = true;
                }
            }

            var normalized = builder.ToString().Trim('_');
            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ValidationException("Transaction type name is invalid.");
            }

            return normalized;
        }
    }
}
