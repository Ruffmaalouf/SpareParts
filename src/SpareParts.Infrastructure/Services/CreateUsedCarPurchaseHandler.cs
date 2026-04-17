using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Services
{
    public sealed class CreateUsedCarPurchaseHandler : ICreateUsedCarPurchaseHandler
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly SupplierAccountResolver _supplierAccountResolver;

        public CreateUsedCarPurchaseHandler(
            ISqlConnectionFactory factory,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IPaymentStatusPolicy paymentStatusPolicy,
            AccountingSettingsProvider settingsProvider,
            SupplierAccountResolver supplierAccountResolver)
        {
            _factory = factory;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _paymentStatusPolicy = paymentStatusPolicy;
            _settingsProvider = settingsProvider;
            _supplierAccountResolver = supplierAccountResolver;
        }

        public CreateUsedCarPurchaseResponse Handle(CreateUsedCarPurchaseRequest request, int userId)
        {
            ValidateRequest(request);

            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session);
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
            var purchaseNumber = _invoiceNumberGenerator.NextPurchaseNumber();
            var purchase = new UsedCarPurchase
            {
                PurchaseNumber = purchaseNumber,
                UsedCarId = request.UsedCarId,
                SupplierId = request.SupplierId,
                PurchaseDate = request.PurchaseDate,
                BaseCurrencyCode = NormalizeCurrencyCode(request.BaseCurrencyCode, "Base currency code"),
                TotalBaseAmount = totalBaseAmount,
                PaidAmount = decimal.Round(request.PaidAmount, 4, MidpointRounding.AwayFromZero),
                PaymentStatus = _paymentStatusPolicy.Resolve(totalBaseAmount, request.PaidAmount).ToString(),
                Notes = (request.Notes ?? string.Empty).Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Lines = lines
            };

            var purchaseId = repositories.Purchases.UsedCarPurchases.Insert(purchase);
            repositories.Purchases.UsedCarPurchases.InsertLines(purchaseId, lines);
            CreateJournalEntry(repositories.Accounting.Journal, purchase, purchaseId, userId);

            session.Commit();

            return new CreateUsedCarPurchaseResponse
            {
                PurchaseId = purchaseId,
                PurchaseNumber = purchaseNumber,
                TotalBaseAmount = totalBaseAmount,
                PaymentStatus = purchase.PaymentStatus
            };
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

        private void CreateJournalEntry(IJournalRepository journalRepository, UsedCarPurchase purchase, int purchaseId, int userId)
        {
            var creditAccountId = _supplierAccountResolver.ResolveAccountId(purchase.SupplierId)
                ?? _settingsProvider.GetSnapshot().PurchaseOffsetAccountId;

            var journalLines = purchase.Lines
                .Select(line => new JournalLine
                {
                    AccountId = line.AccountId,
                    Debit = line.BaseAmount,
                    Credit = 0m,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                })
                .ToList();

            journalLines.Add(new JournalLine
            {
                AccountId = creditAccountId,
                Debit = 0m,
                Credit = purchase.TotalBaseAmount,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            var totalDebit = decimal.Round(journalLines.Sum(line => line.Debit), 4, MidpointRounding.AwayFromZero);
            var totalCredit = decimal.Round(journalLines.Sum(line => line.Credit), 4, MidpointRounding.AwayFromZero);
            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Used-car purchase journal entry is not balanced.");
            }

            var entry = new JournalEntry
            {
                EntryDate = purchase.PurchaseDate,
                ReferenceType = "UsedCarPurchase",
                ReferenceId = purchaseId,
                Description = $"Used car purchase {purchase.PurchaseNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, journalLines);
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
