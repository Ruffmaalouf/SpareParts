using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class QuoteExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public QuoteExpiryHostedService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await MarkExpiredQuotesAsync();

        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await MarkExpiredQuotesAsync();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }

    private async Task MarkExpiredQuotesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ISqlConnectionFactory>();
        using var conn = factory.CreateConnection();
        await conn.ExecuteAsync(
            """
            UPDATE dbo.Quotes
            SET Status = 'Expired'
            WHERE Status IN ('Draft', 'Sent')
              AND ExpiryDate IS NOT NULL
              AND CAST(ExpiryDate AS DATE) < CAST(GETUTCDATE() AS DATE);
            """);
    }
}
