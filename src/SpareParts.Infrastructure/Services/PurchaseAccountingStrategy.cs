using SpareParts.Domain.Accounting;
using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseAccountingStrategy : IAccountingStrategy<PurchaseInvoice>
    {
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly SupplierAccountResolver _supplierAccountResolver;

        public PurchaseAccountingStrategy(AccountingSettingsProvider settingsProvider, SupplierAccountResolver supplierAccountResolver)
        {
            _settingsProvider = settingsProvider;
            _supplierAccountResolver = supplierAccountResolver;
        }

        public List<JournalLine> BuildJournalLines(PurchaseInvoice purchase, int userId)
        {
            if (purchase.TotalAmount < 0)
            {
                throw new InvalidOperationException("Purchase journal lines cannot be generated from negative totals.");
            }

            var settings = _settingsProvider.GetSnapshot();
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
