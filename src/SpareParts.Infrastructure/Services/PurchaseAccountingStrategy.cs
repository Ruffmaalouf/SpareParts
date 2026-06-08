using Microsoft.Extensions.Logging;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseAccountingStrategy : IAccountingStrategy<PurchaseInvoice>
    {
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly SupplierAccountResolver _supplierAccountResolver;
        private readonly ILogger<PurchaseAccountingStrategy>? _logger;

        public PurchaseAccountingStrategy(
            AccountingSettingsProvider settingsProvider,
            SupplierAccountResolver supplierAccountResolver,
            ILogger<PurchaseAccountingStrategy>? logger = null)
        {
            _settingsProvider = settingsProvider;
            _supplierAccountResolver = supplierAccountResolver;
            _logger = logger;
        }

        public List<JournalLine> BuildJournalLines(PurchaseInvoice purchase, int userId)
        {
            if (purchase.TotalAmount < 0)
            {
                throw new InvalidOperationException("Purchase journal lines cannot be generated from negative totals.");
            }

            var settings = _settingsProvider.GetSnapshot();
            if (settings.InventoryAccountId <= 0 || settings.PurchaseOffsetAccountId <= 0)
            {
                _logger?.LogWarning(
                    "Purchase accounting auto-post is using unconfigured account IDs " +
                    "(Inventory={Inventory}, PurchaseOffset={PurchaseOffset}). " +
                    "Configure posting settings to ensure correct journal entries.",
                    settings.InventoryAccountId, settings.PurchaseOffsetAccountId);
            }
            var creditAccountId = _supplierAccountResolver.ResolveAccountId(purchase.SupplierId) ?? settings.PurchaseOffsetAccountId;
            var lines = new List<JournalLine>
            {
                new() { AccountId = settings.InventoryAccountId, Debit = purchase.TotalAmount, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = creditAccountId, Debit = 0, Credit = purchase.TotalAmount, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId }
            };

            var totalDebit = decimal.Round(lines.Sum(x => x.Debit), 4, MidpointRounding.AwayFromZero);
            var totalCredit = decimal.Round(lines.Sum(x => x.Credit), 4, MidpointRounding.AwayFromZero);
            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Purchase journal entry is not balanced.");
            }

            return lines;
        }
    }
}
