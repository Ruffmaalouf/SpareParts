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

        public PurchaseService(
            ISqlConnectionFactory factory,
            ISparePartsDataContextFactory ctxFactory,
            IInventoryService inventoryService,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IPaymentStatusPolicy paymentStatusPolicy,
            IAccountingStrategy<PurchaseInvoice> accountingStrategy)
        {
            _factory = factory;
            _ctxFactory = ctxFactory;
            _inventoryService = inventoryService;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _paymentStatusPolicy = paymentStatusPolicy;
            _accountingStrategy = accountingStrategy;
        }

        public CreatePurchaseResponse CreatePurchase(CreatePurchaseRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var ctx = _ctxFactory.Create(session);

            ValidateRequest(request);
            var parts = LoadParts(ctx, request);

            var (purchaseItems, subtotal, discountTotal, taxTotal) = BuildPurchaseItems(request.Items, parts, userId);
            var totalAmount = subtotal - discountTotal + taxTotal;
            var purchaseNumber = _invoiceNumberGenerator.NextPurchaseNumber();

            var purchase = new PurchaseInvoice
            {
                PurchaseNumber = purchaseNumber,
                PurchaseDate = request.PurchaseDate,
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                Subtotal = subtotal,
                DiscountAmount = discountTotal,
                TaxAmount = taxTotal,
                TotalAmount = totalAmount,
                PaidAmount = request.PaidAmount,
                PaymentStatus = _paymentStatusPolicy.Resolve(totalAmount, request.PaidAmount),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Items = purchaseItems
            };

            var purchaseId = ctx.InsertPurchaseInvoice(purchase);
            ctx.InsertPurchaseInvoiceItems(purchaseId, purchaseItems);

            AdjustStockForPurchase(ctx, request, purchaseItems, purchaseId, userId);
            CreateJournalEntryForPurchase(ctx, purchase, purchaseId, userId);

            session.Commit();

            return new CreatePurchaseResponse
            {
                PurchaseId = purchaseId,
                PurchaseNumber = purchaseNumber,
                TotalAmount = totalAmount,
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

        private static Dictionary<int, Part> LoadParts(SparePartsDataContext ctx, CreatePurchaseRequest request)
        {
            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = ctx.GetPartsByIds(partIds).ToDictionary(p => p.Id, p => p);

            foreach (var item in request.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                {
                    throw new InvalidOperationException($"Part {item.PartId} not found.");
                }
            }

            return parts;
        }

        private static (List<PurchaseInvoiceItem> Items, decimal Subtotal, decimal DiscountTotal, decimal TaxTotal) BuildPurchaseItems(
            IList<PurchaseItemDto> requestItems,
            IReadOnlyDictionary<int, Part> parts,
            int userId)
        {
            var purchaseItems = new List<PurchaseInvoiceItem>();
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in requestItems)
            {
                _ = parts[item.PartId];

                var baseLine = item.Quantity * item.UnitCost;
                var tax = baseLine * (item.TaxRate / 100m);
                var lineTotal = baseLine + tax;

                subtotal += baseLine;
                taxTotal += tax;

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

            return (purchaseItems, subtotal, discountTotal, taxTotal);
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

        private void CreateJournalEntryForPurchase(SparePartsDataContext ctx, PurchaseInvoice purchase, int purchaseId, int userId)
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
            var entryId = ctx.InsertJournalEntry(entry);
            ctx.InsertJournalLines(entryId, lines);
        }
    }
}
