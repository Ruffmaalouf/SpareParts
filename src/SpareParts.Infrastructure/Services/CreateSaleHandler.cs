using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;

namespace SpareParts.Infrastructure.Services
{
    public class CreateSaleHandler : ICreateSaleHandler
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly IInventoryService _inventoryService;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly IAccountingStrategy<SalesInvoice> _accountingStrategy;
        private readonly IInvoiceTotalsCalculator _totalsCalculator;

        public CreateSaleHandler(
            ISqlConnectionFactory factory,
            IInventoryService inventoryService,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IPaymentStatusPolicy paymentStatusPolicy,
            IAccountingStrategy<SalesInvoice> accountingStrategy,
            IInvoiceTotalsCalculator totalsCalculator)
        {
            _factory = factory;
            _inventoryService = inventoryService;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _paymentStatusPolicy = paymentStatusPolicy;
            _accountingStrategy = accountingStrategy;
            _totalsCalculator = totalsCalculator;
        }

        public CreateSaleResponse Handle(CreateSaleRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var repositories = RepositoryCatalog.For(session);

            var salesRepository = repositories.Sales.Invoices;
            var partsRepository = repositories.MasterData.Parts;
            var inventoryRepository = repositories.Inventory.Stock;
            var journalRepository = repositories.Accounting.Journal;

            ValidateRequest(request);
            var parts = LoadParts(partsRepository, request);
            EnsureStockAvailability(inventoryRepository, request, parts);

            var totals = _totalsCalculator.CalculateSales(request.Items);
            var invoiceNumber = _invoiceNumberGenerator.NextSalesNumber();

            var invoice = new SalesInvoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = request.InvoiceDate,
                CustomerId = request.CustomerId,
                WarehouseId = request.WarehouseId,
                Subtotal = totals.Subtotal,
                DiscountAmount = totals.DiscountTotal,
                TaxAmount = totals.TaxTotal,
                TotalAmount = totals.TotalAmount,
                PaidAmount = request.PaidAmount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = _paymentStatusPolicy.Resolve(totals.TotalAmount, request.PaidAmount).ToString(),
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var (items, totalCost) = BuildSaleItems(request, parts, userId);
            invoice.TotalCost = totalCost;

            var invoiceId = salesRepository.InsertInvoice(invoice);
            salesRepository.InsertItems(invoiceId, items);

            AdjustStockForSale(inventoryRepository, invoiceId, request, items, parts, userId);
            CreateJournalEntryForSale(journalRepository, invoice, invoiceId, userId);

            session.Commit();

            return new CreateSaleResponse
            {
                InvoiceId = invoiceId,
                InvoiceNumber = invoiceNumber,
                TotalAmount = totals.TotalAmount,
                PaymentStatus = invoice.PaymentStatus
            };
        }

        private static void ValidateRequest(CreateSaleRequest request)
        {
            if (request.WarehouseId <= 0)
            {
                throw new ValidationException("Warehouse is required.");
            }

            if (request.PaidAmount < 0)
            {
                throw new ValidationException("Paid amount cannot be negative.");
            }

            InvoiceRequestValidator.ValidateSaleItems(request.Items);
        }

        private static Dictionary<int, Part> LoadParts(IPartsRepository repository, CreateSaleRequest request)
        {
            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            return repository.GetByIds(partIds);
        }

        private void EnsureStockAvailability(
            IInventoryRepository inventoryRepository,
            CreateSaleRequest request,
            IReadOnlyDictionary<int, Part> parts)
        {
            var requestedQuantities = InvoiceRequestValidator.AggregateSaleQuantities(request.Items);

            foreach (var entry in requestedQuantities)
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
            CreateSaleRequest request,
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

        private void AdjustStockForSale(
            IInventoryRepository inventoryRepository,
            int invoiceId,
            CreateSaleRequest request,
            IReadOnlyCollection<SalesInvoiceItem> items,
            IReadOnlyDictionary<int, Part> parts,
            int userId)
        {
            foreach (var item in items)
            {
                var part = parts[item.PartId];
                _inventoryService.AdjustStock(
                    inventoryRepository: inventoryRepository,
                    partId: item.PartId,
                    warehouseId: request.WarehouseId,
                    quantityChange: -item.Quantity,
                    movementType: StockMovementType.Sale,
                    referenceType: DomainReferenceType.Sale,
                    referenceId: invoiceId,
                    unitCost: part.CostPrice,
                    userId: userId);
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
