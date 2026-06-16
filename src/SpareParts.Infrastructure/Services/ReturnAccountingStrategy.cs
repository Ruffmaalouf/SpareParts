using Microsoft.Extensions.Logging;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    public class ReturnAccountingStrategy : IAccountingStrategy<SalesReturn>
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly AccountingSettingsProvider _settingsProvider;
        private readonly CustomerAccountResolver _customerAccountResolver;
        private readonly ILogger<ReturnAccountingStrategy>? _logger;

        public ReturnAccountingStrategy(
            ISqlConnectionFactory factory,
            AccountingSettingsProvider settingsProvider,
            CustomerAccountResolver customerAccountResolver,
            ILogger<ReturnAccountingStrategy>? logger = null)
        {
            _factory = factory;
            _settingsProvider = settingsProvider;
            _customerAccountResolver = customerAccountResolver;
            _logger = logger;
        }

        public List<JournalLine> BuildJournalLines(SalesReturn salesReturn, int userId)
        {
            if (salesReturn.TotalAmount < 0 || salesReturn.TotalCost < 0)
            {
                throw new InvalidOperationException("Return journal lines cannot be generated from negative totals.");
            }

            var settings = _settingsProvider.GetSnapshot();
            if (settings.SalesRevenueAccountId <= 0 || settings.CogsAccountId <= 0 || settings.InventoryAccountId <= 0)
            {
                _logger?.LogWarning(
                    "Sales return accounting auto-post is using unconfigured account IDs " +
                    "(Revenue={Revenue}, COGS={Cogs}, Inventory={Inventory}). " +
                    "Configure posting settings to ensure correct journal entries.",
                    settings.SalesRevenueAccountId, settings.CogsAccountId, settings.InventoryAccountId);
            }

            using var session = new DbSession(_factory);
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);

            // Reverse of the original sale GL:
            // Sale:   Customer DR | Revenue CR | COGS DR | Inventory CR
            // Return: Customer CR | Revenue DR | COGS CR | Inventory DR
            var creditAccountId = _customerAccountResolver.ResolveAccountId(salesReturn.CustomerId) ?? settings.SalesCashAccountId;

            var lines = new List<JournalLine>
            {
                AccountingJournalLineFactory.CreateCounterCurrencyLine(creditAccountId, 0m, salesReturn.TotalAmount, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.SalesRevenueAccountId, salesReturn.TotalAmount, 0m, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.CogsAccountId, 0m, salesReturn.TotalCost, currencyContext, userId),
                AccountingJournalLineFactory.CreateCounterCurrencyLine(settings.InventoryAccountId, salesReturn.TotalCost, 0m, currencyContext, userId)
            };

            var imbalance = lines.Sum(x => x.Debit) - lines.Sum(x => x.Credit);
            if (imbalance != 0m)
            {
                lines[^1].Debit += imbalance;
            }

            return lines;
        }
    }
}
