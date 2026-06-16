using SpareParts.Domain.Accounting;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class CreateSalesReturnHandler
    {
        private const int ReturnNumberMaxAttempts = 5;
        private const string SaleReturnRefundReferenceType = "SaleReturnRefund";

        private readonly ISqlConnectionFactory _factory;
        private readonly IInventoryService _inventoryService;
        private readonly IInvoiceNumberGenerator _invoiceNumberGenerator;
        private readonly IAccountingStrategy<SalesReturn> _accountingStrategy;
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly CustomerAccountResolver _customerAccountResolver;
        private readonly ITenantContext _tenantContext;

        public CreateSalesReturnHandler(
            ISqlConnectionFactory factory,
            IInventoryService inventoryService,
            IInvoiceNumberGenerator invoiceNumberGenerator,
            IAccountingStrategy<SalesReturn> accountingStrategy,
            AccountingSettingsProvider settingsProvider,
            CustomerAccountResolver customerAccountResolver,
            ITenantContext tenantContext)
        {
            _factory = factory;
            _inventoryService = inventoryService;
            _invoiceNumberGenerator = invoiceNumberGenerator;
            _accountingStrategy = accountingStrategy;
            _settingsProvider = settingsProvider;
            _customerAccountResolver = customerAccountResolver;
            _tenantContext = tenantContext;
        }

        public CreateSalesReturnResponse Handle(CreateSalesReturnRequest request, int userId)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var repositories = RepositoryCatalog.For(session);

            var salesRepository = repositories.Sales.Invoices;
            var returnsRepository = repositories.Sales.Returns;
            var inventoryRepository = repositories.Inventory.Stock;
            var partsRepository = repositories.MasterData.Parts;
            var journalRepository = repositories.Accounting.Journal;

            ValidateRequest(request);

            var originalInvoice = salesRepository.GetInvoiceById(request.OriginalInvoiceId)
                ?? throw new NotFoundException($"Sale invoice {request.OriginalInvoiceId} not found.");

            var returnableLines = returnsRepository.GetReturnableLines(request.OriginalInvoiceId);
            ValidateReturnItems(request, returnableLines);

            var parts = LoadParts(partsRepository, request);
            var (items, totalAmount, totalCost) = BuildReturnItems(request, parts, userId);

            ValidateRefundAmount(request.RefundAmount, totalAmount);

            var returnNumber = GenerateUniqueReturnNumber(returnsRepository);

            var salesReturn = new SalesReturn
            {
                ReturnNumber = returnNumber,
                OriginalInvoiceId = request.OriginalInvoiceId,
                ReturnDate = request.ReturnDate,
                CustomerId = originalInvoice.CustomerId,
                WarehouseId = request.WarehouseId,
                TotalAmount = totalAmount,
                TotalCost = totalCost,
                RefundAmount = request.RefundAmount,
                Reason = request.Reason?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var returnId = returnsRepository.InsertReturn(salesReturn);
            returnsRepository.InsertItems(returnId, items);

            RestockReturnedItems(inventoryRepository, returnId, request, items, parts, userId);
            CreateJournalEntryForReturn(journalRepository, salesReturn, returnId, userId);
            CreateRefundJournalEntry(session, journalRepository, salesReturn, returnId, userId);

            session.Commit();

            return new CreateSalesReturnResponse
            {
                ReturnId = returnId,
                ReturnNumber = returnNumber,
                OriginalInvoiceId = request.OriginalInvoiceId,
                CreditAmount = totalAmount,
                RefundAmount = request.RefundAmount
            };
        }

        private static void ValidateRequest(CreateSalesReturnRequest request)
        {
            if (request.OriginalInvoiceId <= 0)
            {
                throw new ValidationException("Original invoice is required.");
            }

            if (request.WarehouseId <= 0)
            {
                throw new ValidationException("Warehouse is required.");
            }

            if (request.RefundAmount < 0)
            {
                throw new ValidationException("Refund amount cannot be negative.");
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ValidationException("At least one item is required.");
            }

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    throw new ValidationException($"Return quantity for part {item.PartId} must be greater than zero.");
                }
            }
        }

        private static void ValidateReturnItems(
            CreateSalesReturnRequest request,
            IReadOnlyList<ReturnableLineDto> returnableLines)
        {
            var returnableMap = returnableLines.ToDictionary(l => l.PartId, l => l.ReturnableQuantity);

            foreach (var item in request.Items)
            {
                if (!returnableMap.TryGetValue(item.PartId, out var returnableQty) || returnableQty <= 0)
                {
                    throw new ConflictException($"Part {item.PartId} has no returnable quantity on invoice {request.OriginalInvoiceId}.");
                }

                if (item.Quantity > returnableQty)
                {
                    throw new ConflictException(
                        $"Return quantity {item.Quantity} for part {item.PartId} exceeds returnable quantity {returnableQty}.");
                }
            }
        }

        private static void ValidateRefundAmount(decimal refundAmount, decimal creditAmount)
        {
            if (refundAmount > creditAmount)
            {
                throw new ValidationException(
                    $"Refund amount {refundAmount:N2} cannot exceed the credit amount {creditAmount:N2}.");
            }
        }

        private string GenerateUniqueReturnNumber(ISalesReturnRepository returnsRepository)
        {
            for (var attempt = 0; attempt < ReturnNumberMaxAttempts; attempt++)
            {
                var returnNumber = _invoiceNumberGenerator.NextSalesReturnNumber();
                if (!returnsRepository.ReturnNumberExists(returnNumber))
                {
                    return returnNumber;
                }
            }

            throw new ConflictException("Failed to generate a unique sales return number after multiple attempts.");
        }

        private static Dictionary<int, Part> LoadParts(IPartsRepository repository, CreateSalesReturnRequest request)
        {
            var partIds = request.Items.Select(i => i.PartId).Distinct().ToList();
            return repository.GetByIds(partIds);
        }

        private static (List<SalesInvoiceItem> Items, decimal TotalAmount, decimal TotalCost) BuildReturnItems(
            CreateSalesReturnRequest request,
            IReadOnlyDictionary<int, Part> parts,
            int userId)
        {
            var items = new List<SalesInvoiceItem>();
            decimal totalAmount = 0;
            decimal totalCost = 0;

            foreach (var item in request.Items)
            {
                var net = decimal.Round(item.Quantity * item.UnitPrice, 2, MidpointRounding.AwayFromZero);
                var tax = decimal.Round(net * (item.TaxRate / 100m), 2, MidpointRounding.AwayFromZero);
                var lineTotal = net + tax;

                items.Add(new SalesInvoiceItem
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TaxRate = item.TaxRate,
                    LineTotal = lineTotal,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                });

                totalAmount += lineTotal;

                if (parts.TryGetValue(item.PartId, out var part))
                {
                    totalCost += decimal.Round(part.CostPrice * item.Quantity, 2, MidpointRounding.AwayFromZero);
                }
            }

            return (items, decimal.Round(totalAmount, 2, MidpointRounding.AwayFromZero), decimal.Round(totalCost, 2, MidpointRounding.AwayFromZero));
        }

        private void RestockReturnedItems(
            IInventoryRepository inventoryRepository,
            int returnId,
            CreateSalesReturnRequest request,
            IReadOnlyCollection<SalesInvoiceItem> items,
            IReadOnlyDictionary<int, Part> parts,
            int userId)
        {
            foreach (var item in items)
            {
                var unitCost = parts.TryGetValue(item.PartId, out var part) ? part.CostPrice : 0m;

                _inventoryService.AdjustStock(
                    inventoryRepository: inventoryRepository,
                    partId: item.PartId,
                    warehouseId: request.WarehouseId,
                    quantityChange: item.Quantity,
                    movementType: StockMovementType.Adjust,
                    referenceType: DomainReferenceType.Sale,
                    referenceId: returnId,
                    unitCost: unitCost,
                    userId: userId);
            }
        }

        private void CreateJournalEntryForReturn(
            IJournalRepository journalRepository,
            SalesReturn salesReturn,
            int returnId,
            int userId)
        {
            var entry = new JournalEntry
            {
                EntryDate = salesReturn.ReturnDate,
                ReferenceType = "SaleReturn",
                ReferenceId = returnId,
                Description = $"Sale return {salesReturn.ReturnNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var lines = _accountingStrategy.BuildJournalLines(salesReturn, userId);
            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }

        private void CreateRefundJournalEntry(
            DbSession session,
            IJournalRepository journalRepository,
            SalesReturn salesReturn,
            int returnId,
            int userId)
        {
            if (salesReturn.RefundAmount <= 0m)
            {
                return;
            }

            var customerAccountId = _customerAccountResolver.ResolveAccountId(salesReturn.CustomerId);
            if (customerAccountId is not > 0)
            {
                return;
            }

            var settings = _settingsProvider.GetSnapshot();
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);

            // Cash out: Customer DR (reducing liability), Cash CR
            var lines = new List<JournalLine>
            {
                AccountingJournalLineFactory.CreateCounterCurrencyLine(customerAccountId.Value, salesReturn.RefundAmount, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.SalesCashAccountId, 0m, salesReturn.RefundAmount, currencyContext, userId)
            };

            var entry = new JournalEntry
            {
                EntryDate = salesReturn.ReturnDate,
                ReferenceType = SaleReturnRefundReferenceType,
                ReferenceId = returnId,
                Description = $"Sale return refund {salesReturn.ReturnNumber}",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            var entryId = journalRepository.InsertEntry(entry);
            journalRepository.InsertLines(entryId, lines);
        }
    }
}
