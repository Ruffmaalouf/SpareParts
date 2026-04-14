using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class UsedCarsService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly CurrenciesService _currenciesService;
    private readonly AppConstantsService _appConstantsService;

    public UsedCarsService(
        ISqlConnectionFactory factory,
        CurrenciesService currenciesService,
        AppConstantsService appConstantsService)
    {
        _factory = factory;
        _currenciesService = currenciesService;
        _appConstantsService = appConstantsService;
    }

    public IEnumerable<UsedCarDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<UsedCarDto>(
            @"SELECT uc.Id,
                     uc.CarModelId,
                     CASE
                         WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                             CASE
                                 WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                                 ELSE cm.Name + N' (' + cm.BodyType + N')'
                             END
                         ELSE
                             CASE
                                 WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                                 ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                             END
                     END AS Car,
                     uc.ModelYear,
                     uc.PriceCurrency,
                     uc.Price,
                     uc.PriceBase,
                     uc.PriceCounter,
                     uc.LocationId,
                     COALESCE(NULLIF(LTRIM(RTRIM(loc.Name)), N''), uc.Location) AS Location,
                     uc.Transportation,
                     uc.IsReceived,
                     uc.IsShipped,
                     uc.PartOut,
                     uc.Shipping,
                     uc.Customs,
                     uc.TotalBeforeShipping,
                     uc.GrandTotalBase,
                     uc.GrandTotalCounter
              FROM dbo.UsedCars uc
              INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
              INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
              LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
              ORDER BY cb.Name, cm.Name, uc.ModelYear DESC, uc.Id DESC;");
    }

    public int Create(CreateUsedCarRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO dbo.UsedCars
                (CarModelId, ModelYear, PriceCurrency, Price, PriceBase, PriceCounter, LocationId, Location, Transportation, IsReceived, IsShipped, PartOut, Shipping, Customs, TotalBeforeShipping, GrandTotalBase, GrandTotalCounter, CreatedByUserId)
              VALUES
                (@CarModelId, @ModelYear, @PriceCurrency, @Price, @PriceBase, @PriceCounter, @LocationId, @Location, @Transportation, @IsReceived, @IsShipped, @PartOut, @Shipping, @Customs, @TotalBeforeShipping, @GrandTotalBase, @GrandTotalCounter, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                snapshot.CarModelId,
                snapshot.ModelYear,
                snapshot.PriceCurrency,
                snapshot.Price,
                snapshot.PriceBase,
                snapshot.PriceCounter,
                snapshot.LocationId,
                snapshot.Location,
                snapshot.Transportation,
                snapshot.IsReceived,
                snapshot.IsShipped,
                snapshot.PartOut,
                snapshot.Shipping,
                snapshot.Customs,
                snapshot.TotalBeforeShipping,
                snapshot.GrandTotalBase,
                snapshot.GrandTotalCounter,
                UserId = userId
            });
    }

    public void Update(int id, CreateUsedCarRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var conn = _factory.CreateConnection();
        var updated = conn.Execute(
            @"UPDATE dbo.UsedCars
              SET CarModelId = @CarModelId,
                  ModelYear = @ModelYear,
                  PriceCurrency = @PriceCurrency,
                  Price = @Price,
                  PriceBase = @PriceBase,
                  PriceCounter = @PriceCounter,
                  LocationId = @LocationId,
                  Location = @Location,
                  Transportation = @Transportation,
                  IsReceived = @IsReceived,
                  IsShipped = @IsShipped,
                  PartOut = @PartOut,
                  Shipping = @Shipping,
                  Customs = @Customs,
                  TotalBeforeShipping = @TotalBeforeShipping,
                  GrandTotalBase = @GrandTotalBase,
                  GrandTotalCounter = @GrandTotalCounter,
                  ModifiedAt = @Now,
                  ModifiedByUserId = @UserId
              WHERE Id = @Id",
            new
            {
                Id = id,
                snapshot.CarModelId,
                snapshot.ModelYear,
                snapshot.PriceCurrency,
                snapshot.Price,
                snapshot.PriceBase,
                snapshot.PriceCounter,
                snapshot.LocationId,
                snapshot.Location,
                snapshot.Transportation,
                snapshot.IsReceived,
                snapshot.IsShipped,
                snapshot.PartOut,
                snapshot.Shipping,
                snapshot.Customs,
                snapshot.TotalBeforeShipping,
                snapshot.GrandTotalBase,
                snapshot.GrandTotalCounter,
                UserId = userId,
                Now = DateTime.UtcNow
            });

        if (updated == 0)
        {
            throw new NotFoundException("Used car not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var deleted = conn.Execute("DELETE FROM dbo.UsedCars WHERE Id = @Id", new { Id = id });
        if (deleted == 0)
        {
            throw new NotFoundException("Used car not found.");
        }
    }

    private UsedCarSnapshot BuildSnapshot(CreateUsedCarRequest request)
    {
        ValidateRequest(request);

        var normalizedPriceCurrency = NormalizeCurrencyCode(request.PriceCurrency)
            ?? throw new ValidationException("Price currency is required.");

        using var conn = _factory.CreateConnection();
        var carModelExists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.CarModels WHERE Id = @Id AND IsActive = 1",
            new { Id = request.CarModelId });
        if (carModelExists == 0)
        {
            throw new ValidationException("Selected car model was not found.");
        }

        var selectedLocation = conn.QuerySingleOrDefault<LocationLookup>(
            @"SELECT TOP (1) LocationID,
                             Name,
                             ShippingFees,
                             ShippingFeesCurrencyCode
              FROM dbo.Location
              WHERE LocationID = @Id;",
            new { Id = request.LocationId });
        if (selectedLocation == null)
        {
            throw new ValidationException("Selected location was not found.");
        }

        var locationCurrencyCode = NormalizeCurrencyCode(selectedLocation.ShippingFeesCurrencyCode)
            ?? throw new ValidationException("Selected location has an invalid shipping currency.");

        var constants = _appConstantsService
            .GetAll()
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);
        var rates = _currenciesService.GetAll().ToList();
        var baseCurrencyCode = ResolveCurrencyCode(constants, "BaseCurrencyCode")
            ?? ResolveCurrencyCode(constants, "DefaultCurrencyCode")
            ?? "USD";
        var counterCurrencyCode = ResolveCurrencyCode(constants, "CounterCurrencyCode")
            ?? baseCurrencyCode;
        var defaultCounterRate = constants.TryGetValue("DefaultCounterRate", out var defaultCounterRateText)
            && decimal.TryParse(defaultCounterRateText, out var parsedDefaultCounterRate)
            && parsedDefaultCounterRate > 0
            ? parsedDefaultCounterRate
            : 1m;

        var priceToBaseRate = ResolveRateToBaseCurrency(
            normalizedPriceCurrency,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (priceToBaseRate <= 0)
        {
            throw new ValidationException($"No conversion rate is configured for {normalizedPriceCurrency}.");
        }

        var counterToBaseRate = ResolveRateToBaseCurrency(
            counterCurrencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (counterToBaseRate <= 0)
        {
            counterToBaseRate = defaultCounterRate > 0 ? defaultCounterRate : 1m;
        }

        var locationToCounterRate = ResolveRateToCounterCurrency(
            locationCurrencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (locationToCounterRate <= 0)
        {
            throw new ValidationException($"No conversion rate is configured for location currency {locationCurrencyCode}.");
        }

        var normalizedPartOut = request.PartOut?.Trim() ?? string.Empty;
        var normalizedLocationName = selectedLocation.Name?.Trim() ?? string.Empty;
        var roundedPrice = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        var roundedShipping = decimal.Round(request.Shipping, 2, MidpointRounding.AwayFromZero);
        var roundedCustoms = decimal.Round(request.Customs, 2, MidpointRounding.AwayFromZero);

        var priceBase = decimal.Round(roundedPrice * priceToBaseRate, 2, MidpointRounding.AwayFromZero);
        var priceCounter = counterToBaseRate > 0
            ? decimal.Round(priceBase / counterToBaseRate, 2, MidpointRounding.AwayFromZero)
            : priceBase;
        var transportation = decimal.Round(selectedLocation.ShippingFees * locationToCounterRate, 2, MidpointRounding.AwayFromZero);
        var totalBeforeShipping = decimal.Round(priceCounter + transportation, 2, MidpointRounding.AwayFromZero);
        var expensesCounterTotal = transportation + roundedShipping + roundedCustoms;
        var grandTotalCounter = decimal.Round(priceCounter + expensesCounterTotal, 2, MidpointRounding.AwayFromZero);
        var grandTotalBase = decimal.Round(priceBase + (expensesCounterTotal * counterToBaseRate), 2, MidpointRounding.AwayFromZero);

        return new UsedCarSnapshot
        {
            CarModelId = request.CarModelId,
            ModelYear = request.ModelYear,
            PriceCurrency = normalizedPriceCurrency,
            Price = roundedPrice,
            PriceBase = priceBase,
            PriceCounter = priceCounter,
            LocationId = selectedLocation.LocationID,
            Location = normalizedLocationName,
            Transportation = transportation,
            IsReceived = request.IsReceived,
            IsShipped = request.IsShipped,
            PartOut = normalizedPartOut,
            Shipping = roundedShipping,
            Customs = roundedCustoms,
            TotalBeforeShipping = totalBeforeShipping,
            GrandTotalBase = grandTotalBase,
            GrandTotalCounter = grandTotalCounter
        };
    }

    private static void ValidateRequest(CreateUsedCarRequest request)
    {
        if (request.CarModelId <= 0)
        {
            throw new ValidationException("Car model is required.");
        }

        if (request.ModelYear <= 0)
        {
            throw new ValidationException("Model year is required.");
        }

        if (request.Price <= 0)
        {
            throw new ValidationException("Price must be greater than zero.");
        }

        if (request.LocationId <= 0)
        {
            throw new ValidationException("Location is required.");
        }

        if (request.Shipping < 0 || request.Customs < 0)
        {
            throw new ValidationException("Expense values cannot be negative.");
        }

        if (request.IsReceived && request.Customs <= 0)
        {
            throw new ValidationException("Customs should be different than 0 when the car is marked as received.");
        }
    }

    private static decimal ResolveRateToBaseCurrency(
        string currencyCode,
        IReadOnlyCollection<CurrencyRateDto> rates,
        string baseCurrencyCode,
        string counterCurrencyCode,
        decimal defaultCounterRate)
    {
        if (string.Equals(currencyCode, baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var rate = rates.FirstOrDefault(item =>
            string.Equals(NormalizeCurrencyCode(item.Code), currencyCode, StringComparison.OrdinalIgnoreCase));
        if (rate == null || rate.RateToUsd <= 0)
        {
            return string.Equals(currencyCode, counterCurrencyCode, StringComparison.OrdinalIgnoreCase)
                ? defaultCounterRate
                : 0m;
        }

        var normalizedBaseCode = NormalizeCurrencyCode(rate.BaseCode) ?? baseCurrencyCode;
        if (string.Equals(currencyCode, normalizedBaseCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        return decimal.Round(1m / rate.RateToUsd, 8, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveRateToCounterCurrency(
        string currencyCode,
        IReadOnlyCollection<CurrencyRateDto> rates,
        string baseCurrencyCode,
        string counterCurrencyCode,
        decimal defaultCounterRate)
    {
        var rateToBaseCurrency = ResolveRateToBaseCurrency(
            currencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (rateToBaseCurrency <= 0)
        {
            return 0m;
        }

        var counterRateToBaseCurrency = ResolveRateToBaseCurrency(
            counterCurrencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (counterRateToBaseCurrency <= 0)
        {
            counterRateToBaseCurrency = defaultCounterRate;
        }

        if (counterRateToBaseCurrency <= 0)
        {
            return 0m;
        }

        return decimal.Round(rateToBaseCurrency / counterRateToBaseCurrency, 8, MidpointRounding.AwayFromZero);
    }

    private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> constants, string key)
    {
        if (!constants.TryGetValue(key, out var value))
        {
            return null;
        }

        return NormalizeCurrencyCode(value);
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

    private sealed class LocationLookup
    {
        public int LocationID { get; init; }
        public string Name { get; init; } = string.Empty;
        public decimal ShippingFees { get; init; }
        public string ShippingFeesCurrencyCode { get; init; } = "USD";
    }

    private sealed class UsedCarSnapshot
    {
        public int CarModelId { get; init; }
        public int ModelYear { get; init; }
        public string PriceCurrency { get; init; } = "USD";
        public decimal Price { get; init; }
        public decimal PriceBase { get; init; }
        public decimal PriceCounter { get; init; }
        public int LocationId { get; init; }
        public string Location { get; init; } = string.Empty;
        public decimal Transportation { get; init; }
        public bool IsReceived { get; init; }
        public bool IsShipped { get; init; }
        public string PartOut { get; init; } = string.Empty;
        public decimal Shipping { get; init; }
        public decimal Customs { get; init; }
        public decimal TotalBeforeShipping { get; init; }
        public decimal GrandTotalBase { get; init; }
        public decimal GrandTotalCounter { get; init; }
    }
}
