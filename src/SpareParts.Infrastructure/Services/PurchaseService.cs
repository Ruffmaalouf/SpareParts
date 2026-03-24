using SpareParts.Domain.Accounting;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ISparePartsDataContextFactory _ctxFactory;
        private readonly IInventoryService _inventoryService;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly IAccountingStrategy<PurchaseInvoice> _accountingStrategy;
        private readonly IInvoiceTotalsCalculator _totalsCalculator;

        public PurchaseService(
            ISqlConnectionFactory factory,
            ISparePartsDataContextFactory ctxFactory,
            IInventoryService inventoryService,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IPaymentStatusPolicy paymentStatusPolicy,
            IAccountingStrategy<PurchaseInvoice> accountingStrategy,
            IInvoiceTotalsCalculator totalsCalculator)
        {
            _factory = factory;
            _ctxFactory = ctxFactory;
            _inventoryService = inventoryService;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _paymentStatusPolicy = paymentStatusPolicy;
            _accountingStrategy = accountingStrategy;
            _totalsCalculator = totalsCalculator;
        }

        public CreatePurchaseResponse CreatePurchase(CreatePurchaseRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var ctx = _ctxFactory.Create(session);
            var purchasesRepository = new PurchasesRepository(ctx);
            var partsRepository = new PartsRepository(ctx);
            var journalRepository = new JournalRepository(ctx);

            ValidateRequest(request);
            var parts = LoadParts(partsRepository, request);

            var (purchaseItems, _) = BuildPurchaseItems(request.Items, parts, userId);
            var totals = _totalsCalculator.CalculatePurchase(request.Items);
            var purchaseNumber = _invoiceNumberGenerator.NextPurchaseNumber();

            var purchase = new PurchaseInvoice
            {
                PurchaseNumber = purchaseNumber,
                PurchaseDate = request.PurchaseDate,
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                Subtotal = totals.Subtotal,
                DiscountAmount = totals.DiscountTotal,
                TaxAmount = totals.TaxTotal,
                TotalAmount = totals.TotalAmount,
                PaidAmount = request.PaidAmount,
                PaymentStatus = _paymentStatusPolicy.Resolve(totals.TotalAmount, request.PaidAmount),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Items = purchaseItems
            };

            var purchaseId = purchasesRepository.InsertInvoice(purchase);
            purchasesRepository.InsertItems(purchaseId, purchaseItems);

            AdjustStockForPurchase(ctx, request, purchaseItems, purchaseId, userId);
            CreateJournalEntryForPurchase(journalRepository, purchase, purchaseId, userId);

            session.Commit();

            return new CreatePurchaseResponse
            {
                PurchaseId = purchaseId,
                PurchaseNumber = purchaseNumber,
                TotalAmount = totals.TotalAmount,
                PaymentStatus = purchase.PaymentStatus
            };
        }

        private static void ValidateRequest(CreatePurchaseRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new InvalidOperationException("Purchase must have at least one item.");
            }
        }

        private static Dictionary<int, Part> LoadParts(IPartsRepository repository, CreatePurchaseRequest request)
        {
            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = repository.GetByIds(partIds);

            foreach (var item in request.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                {
                    throw new InvalidOperationException($"Part {item.PartId} not found.");
                }
            }

            return parts;
        }

        private static (List<PurchaseInvoiceItem> Items, decimal TotalCost) BuildPurchaseItems(
            IList<PurchaseItemDto> requestItems,
            IReadOnlyDictionary<int, Part> parts,
            int userId)
        {
            var purchaseItems = new List<PurchaseInvoiceItem>();
            decimal totalCost = 0;

            foreach (var item in requestItems)
            {
                _ = parts[item.PartId];

                var baseLine = item.Quantity * item.UnitCost;
                var tax = baseLine * (item.TaxRate / 100m);
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

                totalCost += item.UnitCost * item.Quantity;
            }

            return (purchaseItems, totalCost);
        }

        private void AdjustStockForPurchase(
            SparePartsDataContext ctx,
            CreatePurchaseRequest request,
            IReadOnlyCollection<PurchaseInvoiceItem> purchaseItems,
            int purchaseId,
            int userId)
        {
            foreach (var item in purchaseItems)
            {
                _inventoryService.AdjustStock(
                    ctx: ctx,
                    partId: item.PartId,
                    warehouseId: request.WarehouseId,
                    quantityChange: item.Quantity,
                    movementType: StockMovementType.Purchase,
                    referenceType: "Purchase",
                    referenceId: purchaseId,
                    unitCost: item.UnitCost,
                    userId: userId);
            }
        }

        private void CreateJournalEntryForPurchase(IJournalRepository journalRepository, PurchaseInvoice purchase, int purchaseId, int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate = purchase.PurchaseDate,
                ReferenceType = "Purchase",
                ReferenceId = purchaseId,
                Description = $"Purchase {purchase.PurchaseNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var lines = _accountingStrategy.BuildJournalLines(purchase, userId);
            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }
    }
}
