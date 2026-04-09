using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class SalesService
    {
        private readonly ICreateSaleHandler _createSaleHandler;
        private readonly ISqlConnectionFactory _factory;
        private readonly IInventoryService _inventoryService;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly IAccountingStrategy<SalesInvoice> _accountingStrategy;
        private readonly IInvoiceTotalsCalculator _totalsCalculator;

        public SalesService(
            ICreateSaleHandler createSaleHandler,
            ISqlConnectionFactory factory,
            IInventoryService inventoryService,
            IPaymentStatusPolicy paymentStatusPolicy,
            IAccountingStrategy<SalesInvoice> accountingStrategy,
            IInvoiceTotalsCalculator totalsCalculator)
        {
            _createSaleHandler = createSaleHandler;
            _factory = factory;
            _inventoryService = inventoryService;
            _paymentStatusPolicy = paymentStatusPolicy;
            _accountingStrategy = accountingStrategy;
            _totalsCalculator = totalsCalculator;
        }

        public CreateSaleResponse CreateSale(CreateSaleRequest request, int userId)
            => _createSaleHandler.Handle(request, userId);

        public List<SalesInvoiceLookupDto> SearchInvoices(string? query)
        {
            using var session = new DbSession(_factory);
            var salesRepository = new SalesRepository(session);
            return salesRepository.SearchInvoices(query);
        }

        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId)
        {
            using var session = new DbSession(_factory);
            var salesRepository = new SalesRepository(session);
            return salesRepository.GetInvoiceById(invoiceId);
        }

        public bool UpdateInvoice(int invoiceId, UpdateSaleRequest request, int userId)
        {
            using var session = new DbSession(_factory);
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
            CreateJournalEntryForSale(journalRepository, invoice, invoiceId, userId);

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
    }
}
