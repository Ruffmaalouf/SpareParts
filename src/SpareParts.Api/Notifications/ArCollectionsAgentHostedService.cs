using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

/// <summary>
/// Agent #5 — the AR Collections Agent. Once a day, finds sales invoices overdue by more than 30
/// days and sends each customer a WhatsApp payment reminder, without anyone chasing it manually.
/// </summary>
public sealed class ArCollectionsAgentHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ArCollectionsAgentHostedService> _logger;

    public ArCollectionsAgentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ArCollectionsAgentHostedService> logger)
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
            var service = scope.ServiceProvider.GetRequiredService<ArCollectionsService>();

            var overdue = service.GetOverdueInvoices();
            var sent = 0;

            foreach (var invoice in overdue)
            {
                if (cancellationToken.IsCancellationRequested) break;

                try
                {
                    await service.SendReminderAsync(invoice, cancellationToken);
                    service.MarkReminderSent(invoice.TransactionId);
                    sent++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AR Collections Agent failed to remind invoice #{InvoiceId}.", invoice.InvoiceId);
                }
            }

            if (sent > 0)
            {
                _logger.LogInformation("AR Collections Agent: sent {Count} overdue payment reminder(s).", sent);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AR Collections Agent tick failed.");
        }
    }
}
