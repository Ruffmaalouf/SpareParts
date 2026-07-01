using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

/// <summary>
/// Agent #4b — the Buying Advisor Agent. Once a day, analyzes which donor vehicle makes/models
/// are selling fast versus sitting as dead stock, and WhatsApps the shop owner a short digest
/// recommending what to prioritize buying next.
/// </summary>
public sealed class BuyingAdvisorAgentHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BuyingAdvisorAgentHostedService> _logger;

    public BuyingAdvisorAgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BuyingAdvisorAgentHostedService> logger)
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
            var service = scope.ServiceProvider.GetRequiredService<BuyingAdvisorService>();

            if (!service.IsDigestDue())
            {
                return;
            }

            var ownerPhone = service.GetOwnerPhone();
            if (string.IsNullOrWhiteSpace(ownerPhone))
            {
                _logger.LogWarning(
                    "Buying Advisor Agent: no owner WhatsApp number configured (set the '{ConstantKey}' app constant) — skipping today's digest.",
                    BuyingAdvisorService.OwnerPhoneConstantKey);
                return;
            }

            var performance = service.GetDonorVehiclePerformance();
            if (performance.Count == 0)
            {
                return;
            }

            var body = service.ComposeDigest(performance);
            await service.SendDigestAsync(ownerPhone, body, userId: 0, cancellationToken);
            service.RecordDigestSent();

            _logger.LogInformation("Buying Advisor Agent: sent the daily buying digest to the owner.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Buying Advisor Agent tick failed.");
        }
    }
}
