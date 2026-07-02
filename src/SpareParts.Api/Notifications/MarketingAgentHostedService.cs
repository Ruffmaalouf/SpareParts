using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

/// <summary>
/// The Marketing Agent — agent #3 in the shop's automated team. When a part becomes sellable
/// (priced and active by the Pricing Agent), this checks it against open customer part requests
/// and need-board "wanted" ads, and automatically sends a WhatsApp "your part is now available"
/// message to every match — no staff member has to remember who was waiting for what.
/// </summary>
public sealed class MarketingAgentHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MarketingAgentHostedService> _logger;

    public MarketingAgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<MarketingAgentHostedService> logger)
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
            var demandMatching = scope.ServiceProvider.GetRequiredService<DemandMatchingService>();

            var pending = demandMatching.GetPartsPendingMarketing();
            if (pending.Count == 0)
            {
                return;
            }

            // Batch the demand lookup across every pending part in one pair of queries instead of
            // one FindMatches call (two SQL round-trips) per part.
            var matchesByPartName = demandMatching.FindMatchesForParts(
                pending.Select(part => part.Name).ToList());

            foreach (var part in pending)
            {
                if (cancellationToken.IsCancellationRequested) break;
                var matches = matchesByPartName.TryGetValue(part.Name, out var partMatches)
                    ? partMatches
                    : Array.Empty<DemandMatchDto>();
                await ProcessPartAsync(demandMatching, part, matches, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Marketing Agent tick failed.");
        }
    }

    private async Task ProcessPartAsync(
        DemandMatchingService demandMatching,
        PartPendingMarketingDto part,
        IReadOnlyList<DemandMatchDto> matches,
        CancellationToken cancellationToken)
    {
        try
        {
            var notified = 0;

            foreach (var match in matches)
            {
                try
                {
                    await demandMatching.NotifyMatchAsync(part.PartId, match, part.CreatedByUserId, cancellationToken);
                    notified++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Marketing Agent failed to notify {Name} ({SourceType} #{SourceId}) about part #{PartId}.",
                        match.Name,
                        match.SourceType,
                        match.SourceId,
                        part.PartId);
                }
            }

            // Mark scanned regardless of match count, so a part with zero current matches isn't
            // rescanned forever — future demand will be caught by that request/ad's own workflow.
            demandMatching.MarkNotified(part.PartId);

            if (notified > 0)
            {
                _logger.LogInformation(
                    "Marketing Agent: notified {NotifiedCount} waiting customer(s) that part #{PartId} ({PartName}) is now available.",
                    notified,
                    part.PartId,
                    part.Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Marketing Agent failed to process part #{PartId}.", part.PartId);
        }
    }
}
