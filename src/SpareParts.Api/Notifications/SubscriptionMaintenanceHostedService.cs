using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Api.Notifications;

/// <summary>Periodically expires due subscriptions, marks past-due ones, and resets monthly usage counters.</summary>
public sealed class SubscriptionMaintenanceHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionMaintenanceHostedService> _logger;

    public SubscriptionMaintenanceHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionMaintenanceHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RunMaintenance();

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                RunMaintenance();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void RunMaintenance()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
            service.RunMaintenance();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Subscription maintenance run failed.");
        }
    }
}
