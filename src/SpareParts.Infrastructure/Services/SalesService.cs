using SpareParts.Domain.Accounting;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class SalesService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ISparePartsDataContextFactory _ctxFactory;
        private readonly IInventoryService _inventoryService;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IPaymentStatusPolicy _paymentStatusPolicy;
        private readonly IAccountingStrategy<SalesInvoice> _accountingStrategy;

        public SalesService(
            ISqlConnectionFactory factory,
            ISparePartsDataContextFactory ctxFactory,
            IInventoryService inventoryService,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IPaymentStatusPolicy paymentStatusPolicy,
            IAccountingStrategy<SalesInvoice> accountingStrategy)
        {
            _factory = factory;
            _ctxFactory = ctxFactory;
            _inventoryService = inventoryService;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _paymentStatusPolicy = paymentStatusPolicy;
            _accountingStrategy = accountingStrategy;
        }

        public CreateSaleResponse CreateSale(CreateSaleRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var ctx = _ctxFactory.Create(session);

            ValidateRequest(request);
            var parts = LoadParts(ctx, request);
            EnsureStockAvailability(ctx, request, parts);

            var (subtotal, discountTotal, taxTotal) = CalculateTotals(request.Items);
            var totalAmount = subtotal - discountTotal + taxTotal;
            var invoiceNumber = _invoiceNumberGenerator.NextSalesNumber();

            var invoice = new SalesInvoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = request.InvoiceDate,
                CustomerId = request.CustomerId,
                WarehouseId = request.WarehouseId,
                Subtotal = subtotal,
                DiscountAmount = discountTotal,
                TaxAmount = taxTotal,
                TotalAmount = totalAmount,
                PaidAmount = request.PaidAmount,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = _paymentStatusPolicy.Resolve(totalAmount, request.PaidAmount),
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var (items, totalCost) = BuildSaleItems(request, parts, userId);
            invoice.TotalCost = totalCost;

            var invoiceId = ctx.InsertSalesInvoice(invoice);
            ctx.InsertSalesInvoiceItems(invoiceId, items);

            AdjustStockForSale(ctx, invoiceId, request, items, parts, userId);
            CreateJournalEntryForSale(ctx, invoice, invoiceId, userId);

            session.Commit();

            return new CreateSaleResponse
            {
                InvoiceId = invoiceId,
                InvoiceNumber = invoiceNumber,
                TotalAmount = totalAmount,
                PaymentStatus = invoice.PaymentStatus
            };
        }

        private static void ValidateRequest(CreateSaleRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new InvalidOperationException("Invoice must have at least one item.");
            }
        }

        private static Dictionary<int, Part> LoadParts(SparePartsDataContext ctx, CreateSaleRequest request)
        {
            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            return ctx.GetPartsByIds(partIds).ToDictionary(p => p.Id, p => p);
        }

        private void EnsureStockAvailability(
            SparePartsDataContext ctx,
            CreateSaleRequest request,
            IReadOnlyDictionary<int, Part> parts)
        {
            foreach (var item in request.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                {
                    throw new InvalidOperationException($"Part {item.PartId} not found.");
                }

                var available = _inventoryService.GetAvailableStock(ctx, item.PartId, request.WarehouseId);
                if (available < item.Quantity)
                {
                    throw new InvalidOperationException($"Not enough stock for part {item.PartId}. Available: {available}");
                }
            }
        }

        private static (decimal Subtotal, decimal DiscountTotal, decimal TaxTotal) CalculateTotals(IList<SaleItemDto> items)
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in items)
            {
                var baseLine = item.Quantity * item.UnitPrice;
                var net = baseLine - item.DiscountAmount;
                var tax = net * (item.TaxRate / 100m);

                subtotal += baseLine;
                discountTotal += item.DiscountAmount;
                taxTotal += tax;
            }

            return (subtotal, discountTotal, taxTotal);
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
            SparePartsDataContext ctx,
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
                    ctx: ctx,
                    partId: item.PartId,
                    warehouseId: request.WarehouseId,
                    quantityChange: -item.Quantity,
                    movementType: StockMovementType.Sale,
                    referenceType: "Sale",
                    referenceId: invoiceId,
                    unitCost: part.CostPrice,
                    userId: userId);
            }
        }

        private void CreateJournalEntryForSale(SparePartsDataContext ctx, SalesInvoice invoice, int invoiceId, int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate = invoice.InvoiceDate,
                ReferenceType = "Sale",
                ReferenceId = invoiceId,
                Description = $"Sale {invoice.InvoiceNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var lines = _accountingStrategy.BuildJournalLines(invoice, userId);
            var entryId = ctx.InsertJournalEntry(entry);
            ctx.InsertJournalLines(entryId, lines);
        }
    }
}
