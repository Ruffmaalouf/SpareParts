using System.Globalization;
using SpareParts.Domain.MasterData;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Shared "is it time to send today's digest yet" scheduling helper used by the once-a-day
/// WhatsApp digest agents (Buying Advisor, Owner Cockpit). Each caller supplies its own
/// <c>AppConstants</c> key so agents track their own last-sent timestamp independently, but the
/// due/record logic itself — identical across agents — lives in one place.
/// </summary>
public static class DailyDigestScheduler
{
    public const int DefaultMinHoursBetweenDigests = 20;

    public static bool IsDigestDue(
        AppConstantsService appConstantsService,
        string lastDigestSentConstantKey,
        int minHoursBetweenDigests = DefaultMinHoursBetweenDigests)
    {
        var lastSent = appConstantsService.GetAll()
            .FirstOrDefault(c => string.Equals(c.Key, lastDigestSentConstantKey, StringComparison.OrdinalIgnoreCase))
            ?.Value;

        if (string.IsNullOrWhiteSpace(lastSent)) return true;

        if (!DateTime.TryParse(lastSent, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var lastSentAt))
        {
            return true;
        }

        return (DateTime.UtcNow - lastSentAt.ToUniversalTime()).TotalHours >= minHoursBetweenDigests;
    }

    public static void RecordDigestSent(
        AppConstantsService appConstantsService,
        string lastDigestSentConstantKey,
        string description)
        => appConstantsService.Upsert(lastDigestSentConstantKey, new UpsertAppConstantRequest
        {
            Value = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Description = description
        });
}
