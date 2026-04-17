using SpareParts.Domain.Accounting;
using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Services
{
    public class SaleAccountingStrategy : IAccountingStrategy<SalesInvoice>
    {
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly CustomerAccountResolver _customerAccountResolver;

        public SaleAccountingStrategy(AccountingSettingsProvider settingsProvider, CustomerAccountResolver customerAccountResolver)
        {
            _settingsProvider = settingsProvider;
            _customerAccountResolver = customerAccountResolver;
        }

        public List<JournalLine> BuildJournalLines(SalesInvoice invoice, int userId)
        {
            if (invoice.TotalAmount < 0 || invoice.TotalCost < 0)
            {
                throw new InvalidOperationException("Sale journal lines cannot be generated from negative totals.");
            }

            var settings = _settingsProvider.GetSnapshot();
            var debitAccountId = _customerAccountResolver.ResolveAccountId(invoice.CustomerId) ?? settings.SalesCashAccountId;
            var lines = new List<JournalLine>
            {
                new() { AccountId = debitAccountId, Debit = invoice.TotalAmount, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = settings.SalesRevenueAccountId, Debit = 0, Credit = invoice.TotalAmount, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = settings.CogsAccountId, Debit = invoice.TotalCost, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = settings.InventoryAccountId, Debit = 0, Credit = invoice.TotalCost, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId }
            };

            var totalDebit = decimal.Round(lines.Sum(x => x.Debit), 4, MidpointRounding.AwayFromZero);
            var totalCredit = decimal.Round(lines.Sum(x => x.Credit), 4, MidpointRounding.AwayFromZero);
            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Sale journal entry is not balanced.");
            }

            return lines;
        }
    }
}
