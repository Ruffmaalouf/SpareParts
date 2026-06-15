using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class CreateUsedCarPurchaseHandler : ICreateUsedCarPurchaseHandler
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly ITenantContext _tenantContext;

        public CreateUsedCarPurchaseHandler(
            ISqlConnectionFactory factory,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IPaymentStatusPolicy paymentStatusPolicy,
            ITenantContext tenantContext)
        {
            _factory = factory;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _paymentStatusPolicy = paymentStatusPolicy;
            _tenantContext = tenantContext;
        }

        public CreateUsedCarPurchaseResponse Handle(CreateUsedCarPurchaseRequest request, int userId)
        {
            ValidateRequest(request);

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repositories = RepositoryCatalog.For(session);
            var usedCarPurchasesRepository = repositories.Purchases.UsedCarPurchases;
            EnsureSupplierExists(session, request.SupplierId);
            EnsureUsedCarExists(session, request.UsedCarId);

            var accounts = repositories.Accounting.Accounts.GetAll().ToDictionary(account => account.Id);
            var lines = request.Lines
                .Where(line => line.BaseAmount > 0m)
                .Select((line, index) => MapLine(line, index, accounts, userId))
                .ToList();

            if (lines.Count == 0)
            {
                throw new ValidationException("At least one positive used-car purchase line is required.");
            }

            var totalBaseAmount = decimal.Round(lines.Sum(line => line.BaseAmount), 4, MidpointRounding.AwayFromZero);
            var totalCounterAmount = decimal.Round(lines.Sum(line => line.CounterAmount), 4, MidpointRounding.AwayFromZero);
            var existingDraft = usedCarPurchasesRepository.GetDraftByUsedCarId(request.UsedCarId);
            if (existingDraft == null && usedCarPurchasesRepository.HasPostedPurchase(request.UsedCarId))
            {
                throw new ValidationException("This used car already has a posted purchase and cannot be saved as a new draft.");
            }

            var paidBaseAmount = decimal.Round(request.PaidAmount, 4, MidpointRounding.AwayFromZero);
            var paidCounterAmount = decimal.Round(request.PaidCounterAmount, 4, MidpointRounding.AwayFromZero);
            var paymentStatus = ResolvePaymentStatus(totalBaseAmount, totalCounterAmount, paidBaseAmount, paidCounterAmount);

            var purchaseNumber = existingDraft?.PurchaseNumber ?? _invoiceNumberGenerator.NextUsedCarPurchaseNumber();
            var purchase = new UsedCarPurchase
            {
                PurchaseNumber = purchaseNumber,
                UsedCarId = request.UsedCarId,
                SupplierId = request.SupplierId,
                PurchaseDate = request.PurchaseDate,
                BaseCurrencyCode = NormalizeCurrencyCode(request.BaseCurrencyCode, "Base currency code"),
                CounterCurrencyCode = NormalizeCurrencyCode(request.CounterCurrencyCode, "Counter currency code"),
                TotalBaseAmount = totalBaseAmount,
                TotalCounterAmount = totalCounterAmount,
                PaidAmount = paidBaseAmount,
                PaidCounterAmount = paidCounterAmount,
                PaymentStatus = paymentStatus,
                PostingStatus = "Draft",
                Notes = (request.Notes ?? string.Empty).Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Lines = lines
            };

            int purchaseId;
            if (existingDraft == null)
            {
                purchaseId = usedCarPurchasesRepository.Insert(purchase);
                usedCarPurchasesRepository.InsertLines(purchaseId, lines);
            }
            else
            {
                purchaseId = existingDraft.Id;
                if (!usedCarPurchasesRepository.Update(purchaseId, purchase))
                {
                    throw new ValidationException("The linked used-car draft could not be updated.");
                }

                usedCarPurchasesRepository.ReplaceLines(purchaseId, lines);
            }

            session.Commit();

            return new CreateUsedCarPurchaseResponse
            {
                PurchaseId = purchaseId,
                PurchaseNumber = purchaseNumber,
                TotalBaseAmount = totalBaseAmount,
                PaymentStatus = purchase.PaymentStatus,
                PostingStatus = purchase.PostingStatus
            };
        }

        private string ResolvePaymentStatus(
            decimal totalBaseAmount,
            decimal totalCounterAmount,
            decimal paidBaseAmount,
            decimal paidCounterAmount)
        {
            var hasCounterPaymentContext = totalCounterAmount > 0m && paidCounterAmount > 0m;
            return hasCounterPaymentContext
                ? _paymentStatusPolicy.Resolve(totalCounterAmount, paidCounterAmount).ToString()
                : _paymentStatusPolicy.Resolve(totalBaseAmount, paidBaseAmount).ToString();
        }

        private static void ValidateRequest(CreateUsedCarPurchaseRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("Used-car purchase request is required.");
            }

            if (request.UsedCarId <= 0)
            {
                throw new ValidationException("Used car is required.");
            }

            if (request.SupplierId <= 0)
            {
                throw new ValidationException("Supplier is required.");
            }

            if (request.PaidAmount < 0)
            {
                throw new ValidationException("Paid amount cannot be negative.");
            }

            if (request.Lines == null || request.Lines.Count == 0)
            {
                throw new ValidationException("At least one used-car purchase line is required.");
            }
        }

        private static UsedCarPurchaseLine MapLine(
            CreateUsedCarPurchaseLineRequest requestLine,
            int index,
            IReadOnlyDictionary<int, AccountDto> accounts,
            int userId)
        {
            var detailKey = NormalizeLookupLikeKey(requestLine.DetailKey, "Detail key");
            var description = (requestLine.Description ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ValidationException($"Used-car purchase line {index + 1} description is required.");
            }

            if (requestLine.Amount < 0 || requestLine.BaseAmount < 0)
            {
                throw new ValidationException($"Used-car purchase line {index + 1} amounts cannot be negative.");
            }

            if (requestLine.CounterAmount < 0)
            {
                throw new ValidationException($"Used-car purchase line {index + 1} counter amount cannot be negative.");
            }

            if (requestLine.RateToBase <= 0)
            {
                throw new ValidationException($"Used-car purchase line {index + 1} must include a positive exchange rate.");
            }

            if (!accounts.ContainsKey(requestLine.AccountId))
            {
                throw new ValidationException($"Used-car purchase line {index + 1} references an invalid account.");
            }

            return new UsedCarPurchaseLine
            {
                DetailKey = detailKey,
                Description = description,
                Amount = decimal.Round(requestLine.Amount, 4, MidpointRounding.AwayFromZero),
                CurrencyCode = NormalizeCurrencyCode(requestLine.CurrencyCode, $"Currency code for {description}"),
                RateToBase = decimal.Round(requestLine.RateToBase, 8, MidpointRounding.AwayFromZero),
                BaseAmount = decimal.Round(requestLine.BaseAmount, 4, MidpointRounding.AwayFromZero),
                CounterAmount = decimal.Round(requestLine.CounterAmount, 4, MidpointRounding.AwayFromZero),
                AccountId = requestLine.AccountId,
                SortOrder = requestLine.SortOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };
        }

        private static void EnsureSupplierExists(DbSession session, int supplierId)
        {
            var exists = session.Connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.Suppliers WHERE Id = @Id;",
                new { Id = supplierId },
                session.Transaction);

            if (exists == 0)
            {
                throw new ValidationException("Selected supplier was not found.");
            }
        }

        private static void EnsureUsedCarExists(DbSession session, int usedCarId)
        {
            var exists = session.Connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @Id;",
                new { Id = usedCarId },
                session.Transaction);

            if (exists == 0)
            {
                throw new ValidationException("Selected used car was not found.");
            }
        }

        private static string NormalizeCurrencyCode(string? currencyCode, string fieldName)
        {
            var normalized = (currencyCode ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized.Length != 3)
            {
                throw new ValidationException($"{fieldName} is invalid.");
            }

            return normalized;
        }

        private static string NormalizeLookupLikeKey(string? rawValue, string fieldName)
        {
            var input = (rawValue ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ValidationException($"{fieldName} is required.");
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
                throw new ValidationException($"{fieldName} is invalid.");
            }

            return normalized;
        }
    }
}
