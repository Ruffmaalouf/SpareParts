using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Interfaces.Repositories;
using Dapper;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseService
    {
        private const string UsedCarReferenceType = "UsedCar";
        private const string PurchasePaymentReferenceType = "PurchasePayment";
        private const string UsedCarPurchasePaymentReferenceType = "UsedCarPurchasePayment";

        private readonly ICreatePurchaseHandler _createPurchaseHandler;
        private readonly ICreateUsedCarPurchaseHandler _createUsedCarPurchaseHandler;
        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly SupplierAccountResolver _supplierAccountResolver;
        private readonly IInventoryService _inventoryService;
        private readonly IInvoiceTotalsCalculator _totalsCalculator;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;

        public PurchaseService(
            ICreatePurchaseHandler createPurchaseHandler,
            ICreateUsedCarPurchaseHandler createUsedCarPurchaseHandler,
            ISqlConnectionFactory factory,
            AccountingSettingsProvider settingsProvider,
            SupplierAccountResolver supplierAccountResolver,
            IInventoryService inventoryService,
            IInvoiceTotalsCalculator totalsCalculator,
            IPaymentStatusPolicy paymentStatusPolicy)
        {
            _createPurchaseHandler = createPurchaseHandler;
            _createUsedCarPurchaseHandler = createUsedCarPurchaseHandler;
            _factory = factory;
            _settingsProvider = settingsProvider;
            _supplierAccountResolver = supplierAccountResolver;
            _inventoryService = inventoryService;
            _totalsCalculator = totalsCalculator;
            _paymentStatusPolicy = paymentStatusPolicy;
        }

        public CreatePurchaseResponse CreatePurchase(CreatePurchaseRequest request, int userId)
            => _createPurchaseHandler.Handle(request, userId);

        public CreateUsedCarPurchaseResponse CreateUsedCarPurchase(CreateUsedCarPurchaseRequest request, int userId)
            => _createUsedCarPurchaseHandler.Handle(request, userId);

        public List<PurchaseInvoiceLookupDto> SearchPurchases(string? query)
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Purchases.Invoices.SearchPurchases(query);
        }

        public PurchaseInvoiceDetailsDto? GetPurchaseById(int purchaseId)
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Purchases.Invoices.GetInvoiceById(purchaseId);
        }

        public bool UpdatePurchase(int purchaseId, UpdatePurchaseRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session);
            var purchasesRepository = repositories.Purchases.Invoices;
            var inventoryRepository = repositories.Inventory.Stock;
            var partsRepository = repositories.MasterData.Parts;
            var journalRepository = repositories.Accounting.Journal;
            var accountingStrategy = new PurchaseAccountingStrategy(_settingsProvider, _supplierAccountResolver);

            var existingInvoice = purchasesRepository.GetInvoiceById(purchaseId);
            if (existingInvoice == null)
            {
                return false;
            }

            ValidateUpdateRequest(request);
            _ = LoadParts(partsRepository, request);
            EnsureStockAvailabilityForUpdate(_inventoryService, inventoryRepository, existingInvoice, request);

            var totals = _totalsCalculator.CalculatePurchase(request.Items);
            var items = BuildPurchaseItems(request.Items, userId);

            var invoice = new PurchaseInvoice
            {
                PurchaseNumber = existingInvoice.PurchaseNumber,
                ScanCode = existingInvoice.ScanCode,
                PurchaseDate = request.PurchaseDate,
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                Subtotal = totals.Subtotal,
                DiscountAmount = totals.DiscountTotal,
                TaxAmount = totals.TaxTotal,
                TotalAmount = totals.TotalAmount,
                PaidAmount = request.PaidAmount,
                PaymentStatus = _paymentStatusPolicy.Resolve(totals.TotalAmount, request.PaidAmount).ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Items = items
            };

            var updated = purchasesRepository.UpdateInvoice(purchaseId, invoice, items, userId);
            if (!updated)
            {
                return false;
            }

            ApplyStockAdjustmentsForUpdate(_inventoryService, inventoryRepository, existingInvoice, request, purchaseId, userId);
            journalRepository.DeleteEntriesByReference(DomainReferenceType.Purchase.ToString(), purchaseId);
            journalRepository.DeleteEntriesByReference(PurchasePaymentReferenceType, purchaseId);
            CreateJournalEntryForPurchase(accountingStrategy, journalRepository, invoice, purchaseId, userId);
            CreatePaymentJournalEntryForPurchase(journalRepository, invoice, purchaseId, userId);

            session.Commit();
            return true;
        }

        public IReadOnlyList<UsedCarPurchaseSummaryDto> GetUsedCarPurchases()
        {
            using var session = new DbSession(_factory);
            return RepositoryCatalog.For(session).Purchases.UsedCarPurchases.GetAll();
        }

        public UsedCarPurchaseDetailDto GetUsedCarPurchase(int id)
        {
            using var session = new DbSession(_factory);
            var purchase = RepositoryCatalog.For(session).Purchases.UsedCarPurchases.GetDetail(id);
            return purchase ?? throw new NotFoundException("Used-car purchase not found.");
        }

        public PostUsedCarPurchaseResponse PostUsedCarPurchase(int id, int userId)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session);
            var purchase = repositories.Purchases.UsedCarPurchases.GetDetail(id)
                ?? throw new NotFoundException("Used-car purchase not found.");

            if (string.Equals(purchase.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException("This used-car purchase is already posted.");
            }

            if (purchase.Lines.Count == 0)
            {
                throw new ValidationException("Cannot post a used-car purchase without lines.");
            }

            if (!IsUsedCarReceived(session, purchase.UsedCarId))
            {
                throw new ValidationException("Mark the linked used car as received before posting its purchase.");
            }

            var accounts = repositories.Accounting.Accounts.GetAll().ToDictionary(account => account.Id);
            foreach (var line in purchase.Lines)
            {
                if (!accounts.ContainsKey(line.AccountId))
                {
                    throw new ValidationException($"The account for purchase line '{line.Description}' no longer exists.");
                }
            }

            repositories.Accounting.Journal.DeleteEntriesByReference("UsedCarPurchase", id);
            repositories.Accounting.Journal.DeleteEntriesByReference(UsedCarReferenceType, purchase.UsedCarId);
            repositories.Accounting.Journal.DeleteEntriesByReference(UsedCarPurchasePaymentReferenceType, id);
            CreateJournalEntry(repositories.Accounting.Journal, purchase, userId);
            CreatePaymentJournalEntry(repositories.Accounting.Journal, purchase, userId);

            if (!repositories.Purchases.UsedCarPurchases.MarkPosted(id, DateTime.UtcNow, userId))
            {
                throw new ValidationException("The used-car purchase could not be marked as posted.");
            }

            session.Commit();

            return new PostUsedCarPurchaseResponse
            {
                PurchaseId = purchase.Id,
                PurchaseNumber = purchase.PurchaseNumber,
                PostingStatus = "Posted"
            };
        }

        public void DeleteUsedCarPurchase(int id)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session);
            repositories.Accounting.Journal.DeleteEntriesByReference("UsedCarPurchase", id);
            repositories.Accounting.Journal.DeleteEntriesByReference(UsedCarPurchasePaymentReferenceType, id);

            if (!repositories.Purchases.UsedCarPurchases.Delete(id))
            {
                throw new NotFoundException("Used-car purchase not found.");
            }

            session.Commit();
        }

        private void CreateJournalEntry(IJournalRepository journalRepository, UsedCarPurchaseDetailDto purchase, int userId)
        {
            var creditAccountId = _supplierAccountResolver.ResolveAccountId(purchase.SupplierId)
                ?? _settingsProvider.GetSnapshot().PurchaseOffsetAccountId;
            var purchaseCounterRateToBase = purchase.TotalCounterAmount > 0m
                ? decimal.Round(purchase.TotalBaseAmount / purchase.TotalCounterAmount, 8, MidpointRounding.AwayFromZero)
                : 1m;

            var journalLines = purchase.Lines
                .Select(line => new JournalLine
                {
                    AccountId = line.AccountId,
                    Debit = line.BaseAmount,
                    Credit = 0m,
                    CurrencyCode = line.CurrencyCode,
                    OriginalAmount = line.Amount,
                    RateToBase = line.RateToBase,
                    CounterAmount = line.CounterAmount,
                    BaseCurrencyCode = purchase.BaseCurrencyCode,
                    CounterCurrencyCode = purchase.CounterCurrencyCode,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                })
                .ToList();

            journalLines.Add(new JournalLine
            {
                AccountId = creditAccountId,
                Debit = 0m,
                Credit = purchase.TotalBaseAmount,
                CurrencyCode = purchase.CounterCurrencyCode,
                OriginalAmount = purchase.TotalCounterAmount > 0m ? purchase.TotalCounterAmount : purchase.TotalBaseAmount,
                RateToBase = purchaseCounterRateToBase > 0m ? purchaseCounterRateToBase : 1m,
                CounterAmount = purchase.TotalCounterAmount > 0m ? purchase.TotalCounterAmount : purchase.TotalBaseAmount,
                BaseCurrencyCode = purchase.BaseCurrencyCode,
                CounterCurrencyCode = purchase.CounterCurrencyCode,
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
                ReferenceId = purchase.Id,
                Description = AccountingJournalDescriptionFormatter.FormatUsedCarPurchase(purchase.UsedCar, purchase.PurchaseNumber),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, journalLines);
        }

        private void CreatePaymentJournalEntryForPurchase(
            IJournalRepository journalRepository,
            PurchaseInvoice purchase,
            int purchaseId,
            int userId)
        {
            if (purchase.PaidAmount <= 0m)
            {
                return;
            }

            var settings = _settingsProvider.GetSnapshot();
            var supplierAccountId = _supplierAccountResolver.ResolveAccountId(purchase.SupplierId)
                ?? settings.PurchaseOffsetAccountId;

            var lines = new List<JournalLine>
            {
                new()
                {
                    AccountId = supplierAccountId,
                    Debit = purchase.PaidAmount,
                    Credit = 0m,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                },
                new()
                {
                    AccountId = settings.SalesCashAccountId,
                    Debit = 0m,
                    Credit = purchase.PaidAmount,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                }
            };

            var entry = new JournalEntry
            {
                EntryDate = purchase.PurchaseDate,
                ReferenceType = PurchasePaymentReferenceType,
                ReferenceId = purchaseId,
                Description = $"Purchase payment {purchase.PurchaseNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }

        private void CreatePaymentJournalEntry(
            IJournalRepository journalRepository,
            UsedCarPurchaseDetailDto purchase,
            int userId)
        {
            if (purchase.PaidAmount <= 0m)
            {
                return;
            }

            var settings = _settingsProvider.GetSnapshot();
            var supplierAccountId = _supplierAccountResolver.ResolveAccountId(purchase.SupplierId)
                ?? settings.PurchaseOffsetAccountId;
            var paidCounterAmount = purchase.PaidCounterAmount > 0m ? purchase.PaidCounterAmount : purchase.PaidAmount;
            var paidRateToBase = paidCounterAmount > 0m
                ? decimal.Round(purchase.PaidAmount / paidCounterAmount, 8, MidpointRounding.AwayFromZero)
                : 1m;

            var lines = new List<JournalLine>
            {
                new()
                {
                    AccountId = supplierAccountId,
                    Debit = purchase.PaidAmount,
                    Credit = 0m,
                    CurrencyCode = purchase.CounterCurrencyCode,
                    OriginalAmount = paidCounterAmount,
                    RateToBase = paidRateToBase > 0m ? paidRateToBase : 1m,
                    CounterAmount = paidCounterAmount,
                    BaseCurrencyCode = purchase.BaseCurrencyCode,
                    CounterCurrencyCode = purchase.CounterCurrencyCode,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                },
                new()
                {
                    AccountId = settings.SalesCashAccountId,
                    Debit = 0m,
                    Credit = purchase.PaidAmount,
                    CurrencyCode = purchase.CounterCurrencyCode,
                    OriginalAmount = paidCounterAmount,
                    RateToBase = paidRateToBase > 0m ? paidRateToBase : 1m,
                    CounterAmount = paidCounterAmount,
                    BaseCurrencyCode = purchase.BaseCurrencyCode,
                    CounterCurrencyCode = purchase.CounterCurrencyCode,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                }
            };

            var entry = new JournalEntry
            {
                EntryDate = purchase.PurchaseDate,
                ReferenceType = UsedCarPurchasePaymentReferenceType,
                ReferenceId = purchase.Id,
                Description = AccountingJournalDescriptionFormatter.FormatUsedCarPurchasePayment(purchase.UsedCar, purchase.PurchaseNumber),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }

        private static bool IsUsedCarReceived(DbSession session, int usedCarId)
        {
            const string sql = @"SELECT COUNT(1)
                                 FROM dbo.UsedCars
                                 WHERE Id = @Id
                                   AND ISNULL(IsReceived, 0) = 1;";

            return session.Connection.ExecuteScalar<int>(sql, new { Id = usedCarId }, session.Transaction) > 0;
        }

        private static void ValidateUpdateRequest(UpdatePurchaseRequest request)
        {
            if (request.SupplierId <= 0)
            {
                throw new ValidationException("Supplier is required.");
            }

            if (request.WarehouseId <= 0)
            {
                throw new ValidationException("Warehouse is required.");
            }

            if (request.PaidAmount < 0)
            {
                throw new ValidationException("Paid amount cannot be negative.");
            }

            InvoiceRequestValidator.ValidatePurchaseItems(request.Items);
        }

        private static Dictionary<int, Part> LoadParts(IPartsRepository repository, UpdatePurchaseRequest request)
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

        private static List<PurchaseInvoiceItem> BuildPurchaseItems(IList<PurchaseItemDto> requestItems, int userId)
        {
            var purchaseItems = new List<PurchaseInvoiceItem>();

            foreach (var item in requestItems)
            {
                var baseLine = Round2(item.Quantity * item.UnitCost);
                var tax = Round2(baseLine * (item.TaxRate / 100m));
                var lineTotal = baseLine + tax;

                purchaseItems.Add(new PurchaseInvoiceItem
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    TaxRate = item.TaxRate,
                    LineTotal = lineTotal,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                });
            }

            return purchaseItems;
        }

        private static decimal Round2(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

        private void EnsureStockAvailabilityForUpdate(
            IInventoryService inventoryService,
            IInventoryRepository inventoryRepository,
            PurchaseInvoiceDetailsDto existingInvoice,
            UpdatePurchaseRequest request)
        {
            var oldQuantities = existingInvoice.Items
                .GroupBy(item => item.PartId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
            var newQuantities = request.Items
                .GroupBy(item => item.PartId)
                .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

            if (existingInvoice.WarehouseId == request.WarehouseId)
            {
                foreach (var partId in oldQuantities.Keys.Union(newQuantities.Keys))
                {
                    var stockReduction = oldQuantities.GetValueOrDefault(partId) - newQuantities.GetValueOrDefault(partId);
                    if (stockReduction <= 0)
                    {
                        continue;
                    }

                    var available = inventoryService.GetAvailableStock(inventoryRepository, partId, request.WarehouseId);
                    if (available < stockReduction)
                    {
                        throw new ConflictException($"Not enough stock available to reduce purchase quantity for part {partId}. Available: {available}");
                    }
                }

                return;
            }

            foreach (var entry in oldQuantities)
            {
                var available = inventoryService.GetAvailableStock(inventoryRepository, entry.Key, existingInvoice.WarehouseId);
                if (available < entry.Value)
                {
                    throw new ConflictException($"Not enough stock available in warehouse {existingInvoice.WarehouseId} to move part {entry.Key}. Available: {available}");
                }
            }
        }

        private void ApplyStockAdjustmentsForUpdate(
            IInventoryService inventoryService,
            IInventoryRepository inventoryRepository,
            PurchaseInvoiceDetailsDto existingInvoice,
            UpdatePurchaseRequest request,
            int purchaseId,
            int userId)
        {
            var oldLinesByPart = existingInvoice.Items
                .GroupBy(item => item.PartId)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Quantity = group.Sum(item => item.Quantity),
                        UnitCost = group.Sum(item => item.Quantity * item.UnitCost) / Math.Max(group.Sum(item => item.Quantity), 1)
                    });

            var newLinesByPart = request.Items
                .GroupBy(item => item.PartId)
                .ToDictionary(
                    group => group.Key,
                    group => new
                    {
                        Quantity = group.Sum(item => item.Quantity),
                        UnitCost = group.Sum(item => item.Quantity * item.UnitCost) / Math.Max(group.Sum(item => item.Quantity), 1)
                    });

            if (existingInvoice.WarehouseId == request.WarehouseId)
            {
                foreach (var partId in oldLinesByPart.Keys.Union(newLinesByPart.Keys))
                {
                    var newLine = newLinesByPart.GetValueOrDefault(partId);
                    var oldLine = oldLinesByPart.GetValueOrDefault(partId);
                    var quantityDelta = (newLine?.Quantity ?? 0) - (oldLine?.Quantity ?? 0);
                    if (quantityDelta == 0)
                    {
                        continue;
                    }

                    var unitCost = quantityDelta > 0
                        ? newLine?.UnitCost ?? 0m
                        : oldLine?.UnitCost ?? 0m;

                    inventoryService.AdjustStock(
                        inventoryRepository,
                        partId,
                        request.WarehouseId,
                        quantityDelta,
                        StockMovementType.Adjust,
                        DomainReferenceType.Purchase,
                        purchaseId,
                        unitCost,
                        userId);
                }

                return;
            }

            foreach (var entry in oldLinesByPart)
            {
                inventoryService.AdjustStock(
                    inventoryRepository,
                    entry.Key,
                    existingInvoice.WarehouseId,
                    -entry.Value.Quantity,
                    StockMovementType.Adjust,
                    DomainReferenceType.Purchase,
                    purchaseId,
                    entry.Value.UnitCost,
                    userId);
            }

            foreach (var entry in newLinesByPart)
            {
                inventoryService.AdjustStock(
                    inventoryRepository,
                    entry.Key,
                    request.WarehouseId,
                    entry.Value.Quantity,
                    StockMovementType.Adjust,
                    DomainReferenceType.Purchase,
                    purchaseId,
                    entry.Value.UnitCost,
                    userId);
            }
        }

        private static void CreateJournalEntryForPurchase(
            IAccountingStrategy<PurchaseInvoice> accountingStrategy,
            IJournalRepository journalRepository,
            PurchaseInvoice purchase,
            int purchaseId,
            int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate = purchase.PurchaseDate,
                ReferenceType = DomainReferenceType.Purchase.ToString(),
                ReferenceId = purchaseId,
                Description = $"Purchase {purchase.PurchaseNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var lines = accountingStrategy.BuildJournalLines(purchase, userId);
            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }
    }
}
