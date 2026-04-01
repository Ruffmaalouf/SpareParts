using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class CurrenciesService
{
    private readonly ISqlConnectionFactory _factory;

    public CurrenciesService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<CurrencyRateDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<CurrencyRateDto>(
            @"SELECT Code, RateToUsd, BaseCode, SnapshotUtc
              FROM CurrencyRates
              ORDER BY Code;");
    }
}
