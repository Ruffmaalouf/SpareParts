using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;

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
            var salesRepository = new SalesRepository(session);
            var partsRepository = new PartsRepository(session);
            var inventoryRepository = new InventoryRepository(session);
            var journalRepository = new JournalRepository(session);

            ValidateRequest(request);
            var parts = LoadParts(partsRepository, request);
            EnsureStockAvailability(inventoryRepository, request, parts);

            var totals = _totalsCalculator.CalculateSales(request.Items);
            var invoiceNumber = GenerateUniqueSalesNumber(salesRepository);

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

        private string GenerateUniqueSalesNumber(ISalesRepository salesRepository)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                var candidate = _invoiceNumberGenerator.NextSalesNumber();
                if (!salesRepository.InvoiceNumberExists(candidate))
                {
                    return candidate;
                }
            }

            throw new ConflictException("Failed to generate a unique sales invoice number after multiple attempts.");
        }

        private static void ValidateRequest(CreateSaleRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ValidationException("Invoice must have at least one item.");
            }
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
            foreach (var item in request.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                {
                    throw new NotFoundException($"Part {item.PartId} not found.");
                }

                var available = _inventoryService.GetAvailableStock(inventoryRepository, item.PartId, request.WarehouseId);
                if (available < item.Quantity)
                {
                    throw new ConflictException($"Not enough stock for part {item.PartId}. Available: {available}");
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
