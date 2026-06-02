using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

public sealed class ReportBuilderBackgroundRunHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportBuilderBackgroundRunHostedService> _logger;

    public ReportBuilderBackgroundRunHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReportBuilderBackgroundRunHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        await RunQueuedReportsAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunQueuedReportsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunQueuedReportsAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ReportBuilderService>();
            service.RequeueStaleBackgroundRuns();

            while (!cancellationToken.IsCancellationRequested
                && service.ProcessQueuedBackgroundRuns(maxRuns: 1) > 0)
            {
                await Task.Yield();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Background report queue processing failed.");
        }
    }
}
