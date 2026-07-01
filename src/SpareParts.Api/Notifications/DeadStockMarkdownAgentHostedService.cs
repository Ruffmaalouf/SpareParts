using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

/// <summary>
/// Agent #4a — the Dead Stock Markdown Agent. Automatically steps a part's price down
/// (SalePrice → FastSalePrice → MinimumSellPrice, never below the floor) the longer it sits
/// unsold, so inventory keeps moving without anyone remembering to discount it manually.
/// </summary>
public sealed class DeadStockMarkdownAgentHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(6);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadStockMarkdownAgentHostedService> _logger;

    public DeadStockMarkdownAgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeadStockMarkdownAgentHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        RunOnce();

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                RunOnce();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private void RunOnce()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<DeadStockMarkdownService>();

            var candidates = service.GetMarkdownCandidates();
            var markedDown = 0;

            foreach (var part in candidates)
            {
                try
                {
                    if (service.ApplyMarkdownIfDue(part, out var newPrice))
                    {
                        markedDown++;
                        _logger.LogInformation(
                            "Dead Stock Agent: marked down part #{PartId} ({Name}) to {NewPrice}.",
                            part.PartId,
                            part.Name,
                            newPrice);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Dead Stock Agent failed to mark down part #{PartId}.", part.PartId);
                }
            }

            if (markedDown > 0)
            {
                _logger.LogInformation("Dead Stock Agent: marked down {Count} slow-moving part(s) this pass.", markedDown);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Dead Stock Agent tick failed.");
        }
    }
}
