using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

/// <summary>
/// The Pricing Agent — agent #2 in the shop's automated team. Watches for draft parts the
/// Intake &amp; Teardown Agent created (still <c>PricingStatus = "Manual"</c>) and, without any
/// manual input, prices and activates them so they're immediately sellable. Each used car's
/// batch of draft parts is priced exactly once, together, so the proportional cost-allocation
/// math never gets re-run over parts that may have already sold.
/// </summary>
public sealed class PricingAgentHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PricingAgentHostedService> _logger;

    public PricingAgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<PricingAgentHostedService> logger)
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

    private Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var pricingService = scope.ServiceProvider.GetRequiredService<PartAutoPricingService>();

            var pending = pricingService.GetPendingPricingParts();
            if (pending.Count == 0) return Task.CompletedTask;

            foreach (var batch in pending.GroupBy(p => p.UsedCarId))
            {
                if (cancellationToken.IsCancellationRequested) break;
                ProcessBatch(pricingService, batch.Key, batch.ToList());
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pricing Agent tick failed.");
        }

        return Task.CompletedTask;
    }

    private void ProcessBatch(PartAutoPricingService pricingService, int usedCarId, List<PendingPricingPartDto> parts)
    {
        try
        {
            var pricedCount = pricingService.PriceCarBatch(usedCarId, parts);

            _logger.LogInformation(
                "Pricing Agent: priced and activated {PricedCount} of {TotalCount} draft parts for used car #{UsedCarId}.",
                pricedCount,
                parts.Count,
                usedCarId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pricing Agent failed to price draft parts for used car #{UsedCarId}.", usedCarId);
        }
    }
}
