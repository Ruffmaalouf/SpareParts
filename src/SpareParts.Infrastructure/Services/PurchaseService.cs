using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TaxRate { get; set; }
    }

    public class CreatePurchaseRequest
    {
        public DateTime PurchaseDate { get; set; }
        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public List<PurchaseItemDto> Items { get; set; } = new List<PurchaseItemDto>();
    }

    public class CreatePurchaseResponse
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }

    public class PurchaseService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly IAccountingStrategy<PurchaseInvoice> _accountingStrategy;

        public PurchaseService(ISqlConnectionFactory factory, IAccountingStrategy<PurchaseInvoice> accountingStrategy)
        {
            _factory = factory;
            _accountingStrategy = accountingStrategy;
        }

        public CreatePurchaseResponse CreatePurchase(CreatePurchaseRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var inventoryService = new InventoryService(ctx);

            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Purchase must have at least one item.");

            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = ctx.GetPartsByIds(partIds).ToDictionary(p => p.Id, p => p);

            foreach (var item in request.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                    throw new InvalidOperationException($"Part {item.PartId} not found.");
            }

            decimal subtotal = 0;
            decimal discountTotal = 0; // for now 0, can extend later
            decimal taxTotal = 0;

            var purchaseItems = new List<PurchaseInvoiceItem>();

            foreach (var item in request.Items)
            {
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

            var totalAmount = subtotal - discountTotal + taxTotal;
            var purchaseNumber = GeneratePurchaseNumber();

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
                PaymentStatus = request.PaymentStatus,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId,
                Items = purchaseItems
            };

            var purchaseId = ctx.InsertPurchaseInvoice(purchase);
            ctx.InsertPurchaseInvoiceItems(purchaseId, purchaseItems);

            // Increase stock
            foreach (var item in request.Items)
            {
                inventoryService.AdjustStock(
                    partId: item.PartId,
                    warehouseId: request.WarehouseId,
                    quantityChange: item.Quantity,
                    movementType: StockMovementType.Purchase,
                    referenceType: "Purchase",
                    referenceId: purchaseId,
                    unitCost: item.UnitCost,
                    userId: userId);
            }

            // Accounting
            CreateJournalEntryForPurchase(ctx, purchase, userId);

            session.Commit();

            return new CreatePurchaseResponse
            {
                PurchaseId = purchaseId,
                PurchaseNumber = purchaseNumber,
                TotalAmount = totalAmount,
                PaymentStatus = purchase.PaymentStatus
            };
        }

        private string GeneratePurchaseNumber()
        {
            return $"PUR-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private void CreateJournalEntryForPurchase(SparePartsDataContext ctx, PurchaseInvoice purchase, int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate = purchase.PurchaseDate,
                ReferenceType = "Purchase",
                ReferenceId = purchase.Id,
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
