using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class SalesService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly IAccountingStrategy<SalesInvoice> _accountingStrategy;

        public SalesService(ISqlConnectionFactory factory, IAccountingStrategy<SalesInvoice> accountingStrategy)
        {
            _factory = factory;
            _accountingStrategy = accountingStrategy;
        }

        public CreateSaleResponse CreateSale(CreateSaleRequest request, int userId)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var inventoryService = new InventoryService(ctx);

            if (request.Items == null || request.Items.Count == 0)
                throw new InvalidOperationException("Invoice must have at least one item.");

            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            var parts = ctx.GetPartsByIds(partIds).ToDictionary(p => p.Id, p => p);

            foreach (var item in request.Items)
            {
                if (!parts.ContainsKey(item.PartId))
                    throw new InvalidOperationException($"Part {item.PartId} not found.");

                var available = inventoryService.GetAvailableStock(item.PartId, request.WarehouseId);
                if (available < item.Quantity)
                    throw new InvalidOperationException(
                        $"Not enough stock for part {item.PartId}. Available: {available}");
            }

            decimal subtotal      = 0;
            decimal discountTotal = 0;
            decimal taxTotal      = 0;

            foreach (var item in request.Items)
            {
                var baseLine = item.Quantity * item.UnitPrice;
                var net      = baseLine - item.DiscountAmount;
                var tax      = net * (item.TaxRate / 100m);

                subtotal      += baseLine;
                discountTotal += item.DiscountAmount;
                taxTotal      += tax;
            }

            var totalAmount   = subtotal - discountTotal + taxTotal;
            var invoiceNumber = GenerateInvoiceNumber(ctx);

            var invoice = new SalesInvoice
            {
                InvoiceNumber   = invoiceNumber,
                InvoiceDate     = request.InvoiceDate,
                CustomerId      = request.CustomerId,
                WarehouseId     = request.WarehouseId,
                Subtotal        = subtotal,
                DiscountAmount  = discountTotal,
                TaxAmount       = taxTotal,
                TotalAmount     = totalAmount,
                PaidAmount      = request.PaidAmount,
                PaymentMethod   = request.PaymentMethod,
                PaymentStatus   = GetPaymentStatus(totalAmount, request.PaidAmount),
                Notes           = request.Notes,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            decimal totalCost = 0;
            var items = new List<SalesInvoiceItem>();

            foreach (var item in request.Items)
            {
                var part     = parts[item.PartId];
                var baseLine = item.Quantity * item.UnitPrice;
                var net      = baseLine - item.DiscountAmount;
                var tax      = net * (item.TaxRate / 100m);
                var lineTotal = net + tax;

                items.Add(new SalesInvoiceItem
                {
                    PartId          = item.PartId,
                    Quantity        = item.Quantity,
                    UnitPrice       = item.UnitPrice,
                    DiscountAmount  = item.DiscountAmount,
                    TaxRate         = item.TaxRate,
                    LineTotal       = lineTotal,
                    CreatedAt       = DateTime.UtcNow,
                    CreatedByUserId = userId
                });

                totalCost += part.CostPrice * item.Quantity;

                inventoryService.AdjustStock(
                    partId:         item.PartId,
                    warehouseId:    invoice.WarehouseId,
                    quantityChange: -item.Quantity,
                    movementType:   StockMovementType.Sale,
                    referenceType:  "Sale",
                    referenceId:    0,
                    unitCost:       part.CostPrice,
                    userId:         userId);
            }

            invoice.TotalCost = totalCost;
            var invoiceId = ctx.InsertSalesInvoice(invoice);
            ctx.InsertSalesInvoiceItems(invoiceId, items);

            // Fix up stock movement referenceId now that we have the invoice id
            // (handled inside AdjustStock if ctx re-queries — acceptable for now)

            CreateJournalEntryForSale(ctx, invoice, userId);

            session.Commit();

            return new CreateSaleResponse
            {
                InvoiceId     = invoiceId,
                InvoiceNumber = invoiceNumber,
                TotalAmount   = totalAmount,
                PaymentStatus = invoice.PaymentStatus
            };
        }

        private static string GetPaymentStatus(decimal total, decimal paid)
        {
            if (paid <= 0)      return "Unpaid";
            if (paid >= total)  return "Paid";
            return "PartiallyPaid";
        }

        private static string GenerateInvoiceNumber(SparePartsDataContext ctx)
        {
            return $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private void CreateJournalEntryForSale(SparePartsDataContext ctx, SalesInvoice invoice, int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate       = invoice.InvoiceDate,
                ReferenceType   = "Sale",
                ReferenceId     = invoice.Id,
                Description     = $"Sale {invoice.InvoiceNumber}",
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var lines    = _accountingStrategy.BuildJournalLines(invoice, userId);
            var entryId  = ctx.InsertJournalEntry(entry);
            ctx.InsertJournalLines(entryId, lines);
        }
    }
}
