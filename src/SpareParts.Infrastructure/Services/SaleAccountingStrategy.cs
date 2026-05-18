using SpareParts.Domain.Accounting;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class SaleAccountingStrategy : IAccountingStrategy<SalesInvoice>
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly CustomerAccountResolver _customerAccountResolver;

        public SaleAccountingStrategy(
            ISqlConnectionFactory factory,
            AccountingSettingsProvider settingsProvider,
            CustomerAccountResolver customerAccountResolver)
        {
            _factory = factory;
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
            using var session = new DbSession(_factory);
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);
            var debitAccountId = _customerAccountResolver.ResolveAccountId(invoice.CustomerId) ?? settings.SalesCashAccountId;
            var lines = new List<JournalLine>
            {
                AccountingJournalLineFactory.CreateCounterCurrencyLine(debitAccountId, invoice.TotalAmount, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.SalesRevenueAccountId, 0m, invoice.TotalAmount, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.CogsAccountId, invoice.TotalCost, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.InventoryAccountId, 0m, invoice.TotalCost, currencyContext, userId)
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
