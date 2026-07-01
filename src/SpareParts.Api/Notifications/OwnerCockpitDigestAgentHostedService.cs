using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

/// <summary>
/// Agent #6 — the Owner Cockpit Agent. Once a day, sends the shop owner a WhatsApp digest of the
/// same data that powers the live Owner Cockpit dashboard: today's sales, cash position, and
/// anything needing attention — one message, no need to log in and check.
/// </summary>
public sealed class OwnerCockpitDigestAgentHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OwnerCockpitDigestAgentHostedService> _logger;

    public OwnerCockpitDigestAgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<OwnerCockpitDigestAgentHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<OwnerCockpitDigestService>();

            if (!service.IsDigestDue())
            {
                return;
            }

            var ownerPhone = service.GetOwnerPhone();
            if (string.IsNullOrWhiteSpace(ownerPhone))
            {
                _logger.LogWarning(
                    "Owner Cockpit Agent: no owner WhatsApp number configured (set the '{ConstantKey}' app constant) — skipping today's digest.",
                    BuyingAdvisorService.OwnerPhoneConstantKey);
                return;
            }

            var dashboard = service.GetDashboard();
            var body = service.ComposeDigest(dashboard);

            await service.SendDigestAsync(ownerPhone, body, cancellationToken);
            service.RecordDigestSent();

            _logger.LogInformation("Owner Cockpit Agent: sent the daily owner digest.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Owner Cockpit Agent tick failed.");
        }
    }
}
