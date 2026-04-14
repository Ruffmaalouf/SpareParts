using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class LocationsService
{
    private readonly ISqlConnectionFactory _factory;

    public LocationsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<LocationDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<LocationDto>(
            @"SELECT LocationID,
                     Name,
                     ShippingFees,
                     ShippingFeesCurrencyCode
              FROM dbo.Location
              ORDER BY Name, LocationID;");
    }

    public int Create(CreateLocationRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO dbo.Location (Name, ShippingFees, ShippingFeesCurrencyCode, CreatedAt, CreatedByUserId)
              VALUES (@Name, @ShippingFees, @ShippingFeesCurrencyCode, @Now, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                snapshot.Name,
                snapshot.ShippingFees,
                snapshot.ShippingFeesCurrencyCode,
                Now = DateTime.UtcNow,
                UserId = userId
            });
    }

    public void Update(int id, CreateLocationRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var conn = _factory.CreateConnection();
        var affected = conn.Execute(
            @"UPDATE dbo.Location
              SET Name = @Name,
                  ShippingFees = @ShippingFees,
                  ShippingFeesCurrencyCode = @ShippingFeesCurrencyCode,
                  ModifiedAt = @Now,
                  ModifiedByUserId = @UserId
              WHERE LocationID = @Id;",
            new
            {
                Id = id,
                snapshot.Name,
                snapshot.ShippingFees,
                snapshot.ShippingFeesCurrencyCode,
                Now = DateTime.UtcNow,
                UserId = userId
            });

        if (affected == 0)
        {
            throw new NotFoundException("Location not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var usedCarsCount = conn.ExecuteScalar<int>(
            @"IF OBJECT_ID('dbo.UsedCars', 'U') IS NULL OR COL_LENGTH('dbo.UsedCars', 'LocationId') IS NULL
                  SELECT 0;
              ELSE
                  SELECT COUNT(1) FROM dbo.UsedCars WHERE LocationId = @Id;",
            new { Id = id });
        if (usedCarsCount > 0)
        {
            throw new ValidationException("Location cannot be deleted while used cars reference it.");
        }

        var affected = conn.Execute("DELETE FROM dbo.Location WHERE LocationId = @Id;", new { Id = id });
        if (affected == 0)
        {
            throw new NotFoundException("Location not found.");
        }
    }

    private static LocationSnapshot BuildSnapshot(CreateLocationRequest request)
    {
        var normalizedName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ValidationException("Location name is required.");
        }

        if (request.ShippingFees < 0)
        {
            throw new ValidationException("Shipping fees cannot be negative.");
        }

        var normalizedCurrencyCode = NormalizeCurrencyCode(request.ShippingFeesCurrencyCode)
            ?? throw new ValidationException("Shipping fees currency is required.");

        return new LocationSnapshot
        {
            Name = normalizedName,
            ShippingFees = decimal.Round(request.ShippingFees, 2, MidpointRounding.AwayFromZero),
            ShippingFeesCurrencyCode = normalizedCurrencyCode
        };
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        return normalized.Length == 3 ? normalized : null;
    }

    private sealed class LocationSnapshot
    {
        public string Name { get; init; } = string.Empty;
        public decimal ShippingFees { get; init; }
        public string ShippingFeesCurrencyCode { get; init; } = "USD";
    }
}
