using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

internal static class AccountingJournalLineFactory
{
    public static JournalLine CreateCounterCurrencyLine(
        int accountId,
        decimal debitCounterAmount,
        decimal creditCounterAmount,
        AccountingCurrencyContext currencyContext,
        int userId)
    {
        var normalizedDebitCounter = NormalizeAmount(debitCounterAmount);
        var normalizedCreditCounter = NormalizeAmount(creditCounterAmount);
        var counterAmount = normalizedDebitCounter > 0m ? normalizedDebitCounter : normalizedCreditCounter;
        var baseAmount = ConvertCounterToBase(counterAmount, currencyContext);
        var rateToBase = ResolveCounterRateToBase(currencyContext);

        return new JournalLine
        {
            AccountId = accountId,
            Debit = normalizedDebitCounter > 0m ? baseAmount : 0m,
            Credit = normalizedCreditCounter > 0m ? baseAmount : 0m,
            CurrencyCode = currencyContext.CounterCurrencyCode,
            OriginalAmount = counterAmount,
            RateToBase = rateToBase,
            CounterAmount = counterAmount,
            BaseCurrencyCode = currencyContext.BaseCurrencyCode,
            CounterCurrencyCode = currencyContext.CounterCurrencyCode,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };
    }

    public static decimal ConvertCounterToBase(decimal counterAmount, AccountingCurrencyContext currencyContext)
    {
        var normalizedAmount = NormalizeAmount(counterAmount);
        if (normalizedAmount == 0m)
        {
            return 0m;
        }

        if (string.Equals(currencyContext.BaseCurrencyCode, currencyContext.CounterCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedAmount;
        }

        return decimal.Round(normalizedAmount * ResolveCounterRateToBase(currencyContext), 4, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveCounterRateToBase(AccountingCurrencyContext currencyContext)
        => currencyContext.CounterRateToBase > 0m
            ? decimal.Round(currencyContext.CounterRateToBase, 8, MidpointRounding.AwayFromZero)
            : 1m;

    private static decimal NormalizeAmount(decimal amount)
        => decimal.Round(amount < 0m ? 0m : amount, 4, MidpointRounding.AwayFromZero);
}
