using Microsoft.AspNetCore.SignalR;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Notifications;

public sealed class PartReservationClockHostedService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NotificationsHub> _notifications;
    private readonly ILogger<PartReservationClockHostedService> _logger;

    public PartReservationClockHostedService(
        IServiceScopeFactory scopeFactory,
        IHubContext<NotificationsHub> notifications,
        ILogger<PartReservationClockHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _notifications = notifications;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunClockAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunClockAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task RunClockAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<PartRequestsService>();
            var result = service.ProcessReservationDeadlines(DateTime.UtcNow);

            foreach (var reminder in result.Reminders)
            {
                await _notifications.Clients.All.SendAsync(
                    NotificationEvents.ReservationReminder,
                    PartReservationReminderNotification.From(reminder),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reservation clock processing failed.");
        }
    }
}
