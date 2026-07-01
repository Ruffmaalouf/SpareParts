using System.Globalization;
using SpareParts.Domain.Communications;
using SpareParts.Domain.OwnerCockpit;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Backs the Owner Cockpit Agent. Once a day, formats the same dashboard data that powers the
/// live Owner Cockpit screen (<see cref="OwnerCockpitService.GetDashboard"/>) into a short
/// WhatsApp digest for the shop owner — today's sales, cash position, and anything needing
/// attention, in one message. Reuses the "OwnerWhatsAppPhone" app constant the Buying Advisor
/// Agent already introduced, rather than adding a second phone-number setting.
/// </summary>
public sealed class OwnerCockpitDigestService
{
    private const string LastDigestSentConstantKey = "OwnerCockpitLastDigestSentAt";
    private const int MinHoursBetweenDigests = 20;

    private readonly OwnerCockpitService _ownerCockpitService;
    private readonly AppConstantsService _appConstantsService;
    private readonly CommunicationsService _communicationsService;

    public OwnerCockpitDigestService(
        OwnerCockpitService ownerCockpitService,
        AppConstantsService appConstantsService,
        CommunicationsService communicationsService)
    {
        _ownerCockpitService = ownerCockpitService;
        _appConstantsService = appConstantsService;
        _communicationsService = communicationsService;
    }

    public string? GetOwnerPhone()
    {
        var value = _appConstantsService.GetAll()
            .FirstOrDefault(c => string.Equals(c.Key, BuyingAdvisorService.OwnerPhoneConstantKey, StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    public bool IsDigestDue()
        => DailyDigestScheduler.IsDigestDue(_appConstantsService, LastDigestSentConstantKey, MinHoursBetweenDigests);

    public void RecordDigestSent()
        => DailyDigestScheduler.RecordDigestSent(
            _appConstantsService,
            LastDigestSentConstantKey,
            "Set automatically by the Owner Cockpit Agent.");

    public OwnerCockpitDashboardDto GetDashboard()
        => _ownerCockpitService.GetDashboard(DateTime.UtcNow.Date);

    public string ComposeDigest(OwnerCockpitDashboardDto dashboard)
    {
        var lines = new List<string>
        {
            $"Daily shop summary for {dashboard.BusinessDate:yyyy-MM-dd}:",
            $"Sales: {dashboard.TodaySalesCount} transaction(s), {Money(dashboard.TodaySalesAmount, dashboard.CurrencyCode)} ({Money(dashboard.TodaySalesPaidAmount, dashboard.CurrencyCode)} collected).",
            $"Profit today: {Money(dashboard.TodaySalesProfit, dashboard.CurrencyCode)}.",
            $"Cash on hand: {Money(dashboard.CashBalance, dashboard.CurrencyCode)}.",
            $"Customers owe you: {Money(dashboard.CustomerDebt, dashboard.CurrencyCode)}. You owe suppliers: {Money(dashboard.SupplierDebt, dashboard.CurrencyCode)}.",
            $"Stock on hand is worth {Money(dashboard.StockValue, dashboard.CurrencyCode)}."
        };

        if (dashboard.AccountingAlertCount > 0)
        {
            var topAlerts = dashboard.AccountingAlerts
                .Take(3)
                .Select(a => a.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title));
            lines.Add($"{dashboard.AccountingAlertCount} item(s) need your attention: {string.Join("; ", topAlerts)}.");
        }
        else
        {
            lines.Add("Nothing needs your attention today.");
        }

        return string.Join(Environment.NewLine, lines);
    }

    public async Task SendDigestAsync(string ownerPhone, string body, CancellationToken cancellationToken)
    {
        var request = new SendBusinessMessageRequest
        {
            Channel = CommunicationChannel.WhatsApp,
            TemplateKey = CommunicationTemplateKey.FreeText,
            RecipientKind = CommunicationRecipientKind.Manual,
            RecipientPhoneOverride = ownerPhone,
            RecipientNameOverride = "Shop Owner",
            MessageBody = body
        };

        await _communicationsService.SendAsync(request, userId: 0, cancellationToken);
    }

    private static string Money(decimal amount, string currencyCode)
        => $"{amount.ToString("N2", CultureInfo.InvariantCulture)} {currencyCode}";
}
