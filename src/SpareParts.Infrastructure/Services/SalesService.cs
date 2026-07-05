using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class SalesService
    {
        private const string SalePaymentReferenceType = "SalePayment";

        private readonly ICreateSaleHandler _createSaleHandler;
        private readonly ISqlConnectionFactory _factory;
        private readonly IInventoryService _inventoryService;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly IAccountingStrategy<SalesInvoice> _accountingStrategy;
        private readonly IInvoiceTotalsCalculator _totalsCalculator;
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly CustomerAccountResolver _customerAccountResolver;
        private readonly ITenantContext _tenantContext;
        private readonly IDashboardCacheInvalidator? _cacheInvalidator;

        public SalesService(
            ICreateSaleHandler createSaleHandler,
            ISqlConnectionFactory factory,
            IInventoryService inventoryService,
            IPaymentStatusPolicy paymentStatusPolicy,
            IAccountingStrategy<SalesInvoice> accountingStrategy,
            IInvoiceTotalsCalculator totalsCalculator,
            AccountingSettingsProvider settingsProvider,
            CustomerAccountResolver customerAccountResolver,
            ITenantContext tenantContext,
            IDashboardCacheInvalidator? cacheInvalidator = null)
        {
            _createSaleHandler = createSaleHandler;
            _factory = factory;
            _inventoryService = inventoryService;
            _paymentStatusPolicy = paymentStatusPolicy;
            _accountingStrategy = accountingStrategy;
            _totalsCalculator = totalsCalculator;
            _settingsProvider = settingsProvider;
            _customerAccountResolver = customerAccountResolver;
            _tenantContext = tenantContext;
            _cacheInvalidator = cacheInvalidator;
        }

        public CreateSaleResponse CreateSale(CreateSaleRequest request, int userId)
            => _createSaleHandler.Handle(request, userId);

        public List<SalesInvoiceLookupDto> SearchInvoices(string? query)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var salesRepository = new SalesRepository(session);
            return salesRepository.SearchInvoices(query);
        }

        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var salesRepository = new SalesRepository(session);
            return salesRepository.GetInvoiceById(invoiceId);
        }

        public List<SalesProfitHistoryPointDto> GetProfitHistory(int months)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var salesRepository = new SalesRepository(session);
            return salesRepository.GetProfitHistory(months);
        }

        public IssueSalePaymentResponse IssuePayment(int invoiceId, IssueSalePaymentRequest request, int userId)
        {
            if (request.Amount <= 0m)
            {
                throw new ValidationException("Payment amount must be greater than zero.");
            }

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repositories = RepositoryCatalog.For(session);
            var salesRepository = repositories.Sales.Invoices;
            var journalRepository = repositories.Accounting.Journal;

            var invoice = salesRepository.GetInvoiceById(invoiceId)
                ?? throw new NotFoundException("Sale invoice not found.");

            var remainingAmount = decimal.Round(invoice.TotalAmount - invoice.PaidAmount, 4, MidpointRounding.AwayFromZero);
            if (remainingAmount <= 0m)
            {
                throw new ValidationException("This sale invoice is already fully paid.");
            }

            var paymentAmount = decimal.Round(request.Amount, 4, MidpointRounding.AwayFromZero);
            if (paymentAmount > remainingAmount)
            {
                throw new ValidationException($"Payment amount cannot be greater than the remaining balance ({remainingAmount:N2}).");
            }

            var paidAmount = decimal.Round(invoice.PaidAmount + paymentAmount, 4, MidpointRounding.AwayFromZero);
            var paymentStatus = _paymentStatusPolicy.Resolve(invoice.TotalAmount, paidAmount).ToString();
            var updated = salesRepository.ApplyPayment(invoiceId, paidAmount, paymentStatus, request.PaymentMethod, userId);
            if (!updated)
            {
                throw new NotFoundException("Sale invoice not found.");
            }

            CreatePaymentJournalEntryForSale(
                session,
                journalRepository,
                invoice.CustomerId,
                invoice.InvoiceNumber,
                invoiceId,
                request.PaymentDate == default ? DateTime.Today : request.PaymentDate,
                paymentAmount,
                request.Notes,
                userId);

            session.Commit();

            // Write-path invalidation (Report 04 R4): a received payment moves the tenant's cash/receivables
            // KPIs and this customer's balance, so evict both read caches immediately instead of waiting the TTL.
            _cacheInvalidator?.InvalidateTenant(_tenantContext.TenantId);
            if (invoice.CustomerId is > 0)
            {
                _cacheInvalidator?.InvalidateClient(_tenantContext.TenantId, invoice.CustomerId.Value);
            }

            return new IssueSalePaymentResponse
            {
                InvoiceId = invoiceId,
                InvoiceNumber = invoice.InvoiceNumber,
                PaymentAmount = paymentAmount,
                PaidAmount = paidAmount,
                RemainingAmount = decimal.Round(invoice.TotalAmount - paidAmount, 4, MidpointRounding.AwayFromZero),
                PaymentStatus = paymentStatus
            };
        }

        public bool UpdateInvoice(int invoiceId, UpdateSaleRequest request, int userId)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var salesRepository = new SalesRepository(session);
            var inventoryRepository = new InventoryRepository(session);
            var partsRepository = new PartsRepository(session);
            var journalRepository = new JournalRepository(session);

            var existingInvoice = salesRepository.GetInvoiceById(invoiceId);
            if (existingInvoice == null)
            {
                return false;
            }

            ValidateUpdateRequest(request);
            var parts = LoadParts(partsRepository, request);
            EnsureStockAvailabilityForUpdate(inventoryRepository, existingInvoice, request, parts);

            var totals = _totalsCalculator.CalculateSales(request.Items);
            var (items, totalCost) = BuildSaleItems(request, parts, userId);

            var invoice = new SalesInvoice
            {
                InvoiceNumber = existingInvoice.InvoiceNumber,
                ScanCode = existingInvoice.ScanCode,
                InvoiceDate = request.InvoiceDate,
                CustomerId = request.CustomerId,
                WarehouseId = request.WarehouseId,
                Subtotal = totals.Subtotal,
                DiscountAmount = totals.DiscountTotal,
                TaxAmount = totals.TaxTotal,
                TotalAmount = totals.TotalAmount,
                PaidAmount = request.PaidAmount,
                PaymentStatus = _paymentStatusPolicy.Resolve(totals.TotalAmount, request.PaidAmount).ToString(),
                TotalCost = totalCost,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var updated = salesRepository.UpdateInvoice(invoiceId, invoice, items, userId);
            if (!updated)
            {
                return false;
            }

            ApplyStockAdjustmentsForUpdate(inventoryRepository, existingInvoice, request, parts, invoiceId, userId);
            journalRepository.DeleteEntriesByReference(DomainReferenceType.Sale.ToString(), invoiceId);
            journalRepository.DeleteEntriesByReference(SalePaymentReferenceType, invoiceId);
            CreateJournalEntryForSale(journalRepository, invoice, invoiceId, userId);
            CreatePaymentJournalEntryForSale(session, journalRepository, invoice, invoiceId, userId);

            session.Commit();
            return updated;
        }

        private static void ValidateUpdateRequest(UpdateSaleRequest request)
        {
            if (request.WarehouseId <= 0)
            {
                throw new ValidationException("Warehouse is required.");
            }

            if (request.PaidAmount < 0)
            {
                throw new ValidationException("Paid amount cannot be negative.");
            }

            InvoiceRequestValidator.ValidateSaleItems(request.Items, "Invoice must have at least one item.");
        }

        private static Dictionary<int, Part> LoadParts(PartsRepository repository, UpdateSaleRequest request)
        {
            var partIds = request.Items.Select(item => item.PartId).Distinct().ToList();
            var parts = repository.GetByIds(partIds);

            foreach (var partId in partIds)
            {
                if (!parts.ContainsKey(partId))
                {
                    throw new NotFoundException($"Part {partId} not found.");
                }
            }

            return parts;
        }

        private void EnsureStockAvailabilityForUpdate(
            InventoryRepository inventoryRepository,
            SalesInvoiceDetailsDto existingInvoice,
            UpdateSaleRequest request,
            IReadOnlyDictionary<int, Part> parts)
        {
            var newQuantities = InvoiceRequestValidator.AggregateSaleQuantities(request.Items);
            var oldQuantities = existingInvoice.Items
                .GroupBy(item => item.PartId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

            if (existingInvoice.WarehouseId == request.WarehouseId)
            {
                foreach (var entry in newQuantities)
                {
                    if (!parts.ContainsKey(entry.Key))
                    {
                        throw new NotFoundException($"Part {entry.Key} not found.");
                    }

                    var previousQuantity = oldQuantities.GetValueOrDefault(entry.Key);
                    var additionalQuantityRequired = entry.Value - previousQuantity;
                    if (additionalQuantityRequired <= 0)
                    {
                        continue;
                    }

                    var available = _inventoryService.GetAvailableStock(inventoryRepository, entry.Key, request.WarehouseId);
                    if (available < additionalQuantityRequired)
                    {
                        throw new ConflictException($"Not enough stock for part {entry.Key}. Available: {available}");
                    }
                }

                return;
            }

            foreach (var entry in newQuantities)
            {
                if (!parts.ContainsKey(entry.Key))
                {
                    throw new NotFoundException($"Part {entry.Key} not found.");
                }

                var available = _inventoryService.GetAvailableStock(inventoryRepository, entry.Key, request.WarehouseId);
                if (available < entry.Value)
                {
                    throw new ConflictException($"Not enough stock for part {entry.Key}. Available: {available}");
                }
            }
        }

        private static (List<SalesInvoiceItem> Items, decimal TotalCost) BuildSaleItems(
            UpdateSaleRequest request,
            IReadOnlyDictionary<int, Part> parts,
            int userId)
        {
            var items = new List<SalesInvoiceItem>();
            decimal totalCost = 0;

            foreach (var item in request.Items)
            {
                var part = parts[item.PartId];
                var baseLine = item.Quantity * item.UnitPrice;
                var net = baseLine - item.DiscountAmount;
                var tax = net * (item.TaxRate / 100m);
                var lineTotal = net + tax;

                items.Add(new SalesInvoiceItem
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TaxRate = item.TaxRate,
                    LineTotal = lineTotal,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                });

                totalCost += part.CostPrice * item.Quantity;
            }

            return (items, totalCost);
        }

        private void ApplyStockAdjustmentsForUpdate(
            InventoryRepository inventoryRepository,
            SalesInvoiceDetailsDto existingInvoice,
            UpdateSaleRequest request,
            IReadOnlyDictionary<int, Part> parts,
            int invoiceId,
            int userId)
        {
            var oldQuantities = existingInvoice.Items
                .GroupBy(item => item.PartId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
            var newQuantities = InvoiceRequestValidator.AggregateSaleQuantities(request.Items);

            if (existingInvoice.WarehouseId == request.WarehouseId)
            {
                foreach (var partId in oldQuantities.Keys.Union(newQuantities.Keys))
                {
                    var stockDelta = oldQuantities.GetValueOrDefault(partId) - newQuantities.GetValueOrDefault(partId);
                    if (stockDelta == 0)
                    {
                        continue;
                    }

                    _inventoryService.AdjustStock(
                        inventoryRepository,
                        partId,
                        request.WarehouseId,
                        stockDelta,
                        StockMovementType.Adjust,
                        DomainReferenceType.Sale,
                        invoiceId,
                        parts[partId].CostPrice,
                        userId);
                }

                return;
            }

            foreach (var entry in oldQuantities)
            {
                if (entry.Value == 0)
                {
                    continue;
                }

                var unitCost = parts.TryGetValue(entry.Key, out var part)
                    ? part.CostPrice
                    : 0m;

                _inventoryService.AdjustStock(
                    inventoryRepository,
                    entry.Key,
                    existingInvoice.WarehouseId,
                    entry.Value,
                    StockMovementType.Adjust,
                    DomainReferenceType.Sale,
                    invoiceId,
                    unitCost,
                    userId);
            }

            foreach (var entry in newQuantities)
            {
                _inventoryService.AdjustStock(
                    inventoryRepository,
                    entry.Key,
                    request.WarehouseId,
                    -entry.Value,
                    StockMovementType.Adjust,
                    DomainReferenceType.Sale,
                    invoiceId,
                    parts[entry.Key].CostPrice,
                    userId);
            }
        }

        private void CreateJournalEntryForSale(IJournalRepository journalRepository, SalesInvoice invoice, int invoiceId, int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate = invoice.InvoiceDate,
                ReferenceType = DomainReferenceType.Sale.ToString(),
                ReferenceId = invoiceId,
                Description = $"Sale {invoice.InvoiceNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var lines = _accountingStrategy.BuildJournalLines(invoice, userId);
            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }

        private void CreatePaymentJournalEntryForSale(
            DbSession session,
            IJournalRepository journalRepository,
            SalesInvoice invoice,
            int invoiceId,
            int userId)
        {
            CreatePaymentJournalEntryForSale(
                session,
                journalRepository,
                invoice.CustomerId,
                invoice.InvoiceNumber,
                invoiceId,
                invoice.InvoiceDate,
                invoice.PaidAmount,
                null,
                userId,
                allowMissingCustomerAccount: true);
        }

        private void CreatePaymentJournalEntryForSale(
            DbSession session,
            IJournalRepository journalRepository,
            int? customerId,
            string invoiceNumber,
            int invoiceId,
            DateTime paymentDate,
            decimal paymentAmount,
            string? notes,
            int userId,
            bool allowMissingCustomerAccount = false)
        {
            if (paymentAmount <= 0m)
            {
                return;
            }

            var customerAccountId = _customerAccountResolver.ResolveAccountId(customerId);
            if (customerAccountId is not > 0)
            {
                if (allowMissingCustomerAccount)
                {
                    return;
                }

                throw new ValidationException("Cannot issue sale payment because the selected customer has no accounting account.");
            }

            var settings = _settingsProvider.GetSnapshot();
            var currencyContext = ResolveSaleCurrencyContext(session, invoiceId);
            var lines = new List<JournalLine>
            {
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.SalesCashAccountId, paymentAmount, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(customerAccountId.Value, 0m, paymentAmount, currencyContext, userId)
            };

            var entry = new JournalEntry
            {
                EntryDate = paymentDate,
                ReferenceType = SalePaymentReferenceType,
                ReferenceId = invoiceId,
                Description = FormatSalePaymentDescription(invoiceNumber, notes),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }

        private AccountingCurrencyContext ResolveSaleCurrencyContext(DbSession session, int invoiceId)
        {
            var fallback = AccountingCurrencyContextResolver.Resolve(session);
            const string sql = @"SELECT TOP (1)
                                        ISNULL(NULLIF(t.BaseCurrencyCode, N''), @FallbackBaseCurrencyCode) AS BaseCurrencyCode,
                                        ISNULL(NULLIF(t.CounterCurrencyCode, N''), @FallbackCounterCurrencyCode) AS CounterCurrencyCode,
                                        CASE
                                            WHEN ISNULL(t.TotalCounterAmount, 0) > 0 AND ISNULL(t.TotalBaseAmount, 0) > 0
                                            THEN ROUND(t.TotalBaseAmount / t.TotalCounterAmount, 8)
                                            ELSE @FallbackCounterRateToBase
                                        END AS CounterRateToBase
                                 FROM dbo.Transactions t
                                 INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
                                 WHERE tt.TypeKey = @TypeKey
                                   AND t.ReferenceId = @InvoiceId;";

            var context = session.Connection.QuerySingleOrDefault<SalePaymentCurrencyContext>(sql, new
            {
                TypeKey = TransactionTypeKeys.Sale,
                InvoiceId = invoiceId,
                FallbackBaseCurrencyCode = fallback.BaseCurrencyCode,
                FallbackCounterCurrencyCode = fallback.CounterCurrencyCode,
                FallbackCounterRateToBase = fallback.CounterRateToBase
            }, session.Transaction);

            if (context == null)
            {
                return fallback;
            }

            return new AccountingCurrencyContext
            {
                BaseCurrencyCode = NormalizeCurrencyCode(context.BaseCurrencyCode, fallback.BaseCurrencyCode),
                CounterCurrencyCode = NormalizeCurrencyCode(context.CounterCurrencyCode, fallback.CounterCurrencyCode),
                CounterRateToBase = context.CounterRateToBase > 0m
                    ? decimal.Round(context.CounterRateToBase, 8, MidpointRounding.AwayFromZero)
                    : fallback.CounterRateToBase
            };
        }

        private static string NormalizeCurrencyCode(string? value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var normalized = value.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : fallback;
        }

        private static string FormatSalePaymentDescription(string invoiceNumber, string? notes)
        {
            var trimmedNotes = notes?.Trim();
            return string.IsNullOrWhiteSpace(trimmedNotes)
                ? $"Sale payment {invoiceNumber}"
                : $"Sale payment {invoiceNumber} - {trimmedNotes}";
        }

    }
}
