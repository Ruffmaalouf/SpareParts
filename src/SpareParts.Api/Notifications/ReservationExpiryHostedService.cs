using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Notifications;

public sealed class ReservationExpiryHostedService : BackgroundService
{
    private const int StaleReservationDays = 30;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpiryHostedService> _logger;

    public ReservationExpiryHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ExpireStaleReservationsAsync();

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await ExpireStaleReservationsAsync();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task ExpireStaleReservationsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
            using var conn = factory.CreateConnection();

            var cancelled = await conn.ExecuteAsync(
                """
UPDATE pr
SET pr.Status = N'Cancelled'
FROM dbo.PartRequests pr
WHERE pr.Status = N'Reserved'
  AND DATEDIFF(DAY, pr.CreatedAt, GETUTCDATE()) > @StaleReservationDays;

UPDATE s
SET s.ReservedQuantity = ISNULL(s.ReservedQuantity, 0) - r.Quantity
FROM dbo.Stock s
INNER JOIN dbo.PartRequestReservations r ON r.StockId = s.Id
INNER JOIN dbo.PartRequests pr ON pr.Id = r.PartRequestId
WHERE pr.Status = N'Cancelled'
  AND r.Status = N'Active'
  AND DATEDIFF(DAY, pr.CreatedAt, GETUTCDATE()) > @StaleReservationDays;

UPDATE r
SET r.Status = N'Released'
FROM dbo.PartRequestReservations r
INNER JOIN dbo.PartRequests pr ON pr.Id = r.PartRequestId
WHERE pr.Status = N'Cancelled'
  AND r.Status = N'Active'
  AND DATEDIFF(DAY, pr.CreatedAt, GETUTCDATE()) > @StaleReservationDays;
""",
                new { StaleReservationDays });

            if (cancelled > 0)
            {
                _logger.LogInformation("ReservationExpiryJob: cancelled {Count} stale reserved part requests.", cancelled);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReservationExpiryJob failed.");
        }
    }
}
