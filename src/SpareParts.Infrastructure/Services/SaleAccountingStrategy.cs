using Microsoft.Extensions.Logging;
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
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<SaleAccountingStrategy>? _logger;

        public SaleAccountingStrategy(
            ISqlConnectionFactory factory,
            AccountingSettingsProvider settingsProvider,
            CustomerAccountResolver customerAccountResolver,
            ITenantContext tenantContext,
            ILogger<SaleAccountingStrategy>? logger = null)
        {
            _factory = factory;
            _settingsProvider = settingsProvider;
            _customerAccountResolver = customerAccountResolver;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public List<JournalLine> BuildJournalLines(SalesInvoice invoice, int userId)
        {
            if (invoice.TotalAmount < 0 || invoice.TotalCost < 0)
            {
                throw new InvalidOperationException("Sale journal lines cannot be generated from negative totals.");
            }

            var settings = _settingsProvider.GetSnapshot();
            if (settings.SalesRevenueAccountId <= 0 || settings.CogsAccountId <= 0 || settings.InventoryAccountId <= 0)
            {
                _logger?.LogWarning(
                    "Sale accounting auto-post is using unconfigured account IDs " +
                    "(Revenue={Revenue}, COGS={Cogs}, Inventory={Inventory}). " +
                    "Configure posting settings to ensure correct journal entries.",
                    settings.SalesRevenueAccountId, settings.CogsAccountId, settings.InventoryAccountId);
            }
            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);
            var debitAccountId = _customerAccountResolver.ResolveAccountId(invoice.CustomerId) ?? settings.SalesCashAccountId;
            var lines = new List<JournalLine>
            {
                AccountingJournalLineFactory.CreateCounterCurrencyLine(debitAccountId, invoice.TotalAmount, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.SalesRevenueAccountId, 0m, invoice.TotalAmount, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.CogsAccountId, invoice.TotalCost, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.InventoryAccountId, 0m, invoice.TotalCost, currencyContext, userId)
            };

            // Absorb any rounding imbalance from currency conversion into the last (inventory credit) line.
            var imbalance = lines.Sum(x => x.Debit) - lines.Sum(x => x.Credit);
            if (imbalance != 0m)
            {
                lines[^1].Credit += imbalance;
            }

            return lines;
        }
    }
}
