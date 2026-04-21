namespace SpareParts.Infrastructure.Services;

internal static class AccountingJournalDescriptionFormatter
{
    private const int MaxDescriptionLength = 400;

    public static string FormatUsedCarReceipt(string? usedCarDisplayName)
        => Format("Used car receipt", usedCarDisplayName, null);

    public static string FormatUsedCarPurchase(string? usedCarDisplayName, string? purchaseNumber)
        => Format("Used car purchase", usedCarDisplayName, purchaseNumber);

    public static string FormatUsedCarPurchasePayment(string? usedCarDisplayName, string? purchaseNumber)
        => Format("Used car purchase payment", usedCarDisplayName, purchaseNumber);

    private static string Format(string prefix, string? usedCarDisplayName, string? suffix)
    {
        var normalizedUsedCarDisplayName = Normalize(usedCarDisplayName);
        var normalizedSuffix = Normalize(suffix);

        var description = normalizedUsedCarDisplayName switch
        {
            not null when normalizedSuffix is not null => $"{prefix} - {normalizedUsedCarDisplayName} ({normalizedSuffix})",
            not null => $"{prefix} - {normalizedUsedCarDisplayName}",
            _ when normalizedSuffix is not null => $"{prefix} {normalizedSuffix}",
            _ => prefix
        };

        return description.Length <= MaxDescriptionLength
            ? description
            : description[..MaxDescriptionLength];
    }

    private static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
