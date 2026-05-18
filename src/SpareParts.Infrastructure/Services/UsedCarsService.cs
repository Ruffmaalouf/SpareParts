using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Cars;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces.Repositories;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class UsedCarsService
{
    private const string UsedCarReferenceType = "UsedCar";

    private readonly ISqlConnectionFactory _factory;
    private readonly CurrenciesService _currenciesService;
    private readonly AppConstantsService _appConstantsService;
    private readonly AccountingSettingsProvider _accountingSettingsProvider;

    public UsedCarsService(
        ISqlConnectionFactory factory,
        CurrenciesService currenciesService,
        AppConstantsService appConstantsService,
        AccountingSettingsProvider accountingSettingsProvider)
    {
        _factory = factory;
        _currenciesService = currenciesService;
        _appConstantsService = appConstantsService;
        _accountingSettingsProvider = accountingSettingsProvider;
    }

    public IEnumerable<UsedCarDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<UsedCarDto>(
            @"SELECT uc.Id,
                     uc.Barcode,
                     uc.SupplierId,
                     COALESCE(s.Name, N'') AS SupplierName,
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
                     uc.PartOutAmount AS PartOut,
                     uc.Shipping,
                     uc.Customs,
                     uc.Repairs,
                     uc.TotalBeforeShipping,
                     uc.GrandTotalBase,
                     uc.GrandTotalCounter,
                     uc.BaseCurrencyCode,
                     uc.CounterCurrencyCode,
                     uc.CounterRateToBase,
                     cost.PurchaseCostBase,
                     cost.TransportationCostBase,
                     cost.CustomsCostBase,
                     cost.ShippingCostBase,
                     cost.PartOutCostBase,
                     cost.RepairsCostBase,
                     cost.FullCostBase,
                     linked.PartsRemovedCount,
                     linked.PartsRemovedValueBase,
                     sales.PartsSoldQuantity,
                     sales.PartsSoldAmountBase,
                     sales.PartsSoldAmountBase AS SalePriceBase,
                     stock.RemainingStockQuantity,
                     stock.RemainingStockValueBase,
                     profit.NetProfitLossBase
              FROM dbo.UsedCars uc
              LEFT JOIN dbo.Suppliers s ON s.Id = uc.SupplierId
              INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
              INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
              LEFT JOIN dbo.Location loc ON loc.LocationId = uc.LocationId
              CROSS APPLY
              (
                  SELECT CounterRateToBase = CASE WHEN ISNULL(uc.CounterRateToBase, 0) > 0 THEN uc.CounterRateToBase ELSE 1 END
              ) rate
              OUTER APPLY
              (
                  SELECT PartsRemovedCount = COUNT(1),
                         PartsRemovedValueBase = ISNULL(SUM(COALESCE(NULLIF(p.AveragePrice, 0), NULLIF(p.CostPrice, 0), 0)), 0)
                  FROM dbo.Parts p
                  WHERE p.UsedCarId = uc.Id
              ) linked
              OUTER APPLY
              (
                  SELECT PartsSoldQuantity = ISNULL(SUM(CASE WHEN t.IsReturn = 1 THEN -ABS(ISNULL(ti.Quantity, 0)) ELSE ABS(ISNULL(ti.Quantity, 0)) END), 0),
                         PartsSoldAmountBase = ISNULL(SUM(CASE WHEN t.IsReturn = 1 THEN -ISNULL(ti.BaseAmount, ti.LineTotal) ELSE ISNULL(ti.BaseAmount, ti.LineTotal) END), 0)
                  FROM dbo.Transactions t
                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = N'sale'
                  INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
                  INNER JOIN dbo.Parts p ON p.Id = ti.PartId
                  WHERE p.UsedCarId = uc.Id
                    AND ti.PartId IS NOT NULL
              ) sales
              OUTER APPLY
              (
                  SELECT RemainingStockQuantity = ISNULL(SUM(CAST(st.Quantity AS DECIMAL(19, 4))), 0),
                         RemainingStockValueBase = ISNULL(SUM(CAST(st.Quantity AS DECIMAL(19, 4)) * COALESCE(NULLIF(p.AveragePrice, 0), NULLIF(p.CostPrice, 0), 0)), 0)
                  FROM dbo.Parts p
                  LEFT JOIN dbo.Stock st ON st.PartId = p.Id
                  WHERE p.UsedCarId = uc.Id
              ) stock
              CROSS APPLY
              (
                  SELECT PurchaseCostBase = ISNULL(uc.PriceBase, 0),
                         TransportationCostBase = ROUND(ISNULL(uc.Transportation, 0) * rate.CounterRateToBase, 2),
                         CustomsCostBase = ROUND(ISNULL(uc.Customs, 0) * rate.CounterRateToBase, 2),
                         ShippingCostBase = ROUND(ISNULL(uc.Shipping, 0) * rate.CounterRateToBase, 2),
                         PartOutCostBase = ROUND(ISNULL(uc.PartOutAmount, 0) * rate.CounterRateToBase, 2),
                         RepairsCostBase = ROUND(ISNULL(uc.Repairs, 0) * rate.CounterRateToBase, 2),
                         FullCostBase = ROUND(ISNULL(uc.PriceBase, 0) + ((ISNULL(uc.Transportation, 0) + ISNULL(uc.PartOutAmount, 0) + ISNULL(uc.Shipping, 0) + ISNULL(uc.Customs, 0) + ISNULL(uc.Repairs, 0)) * rate.CounterRateToBase), 2)
              ) cost
              CROSS APPLY
              (
                  SELECT NetProfitLossBase = ROUND(ISNULL(sales.PartsSoldAmountBase, 0) + ISNULL(stock.RemainingStockValueBase, 0) - cost.FullCostBase, 2)
              ) profit
              ORDER BY cb.Name, cm.Name, uc.ModelYear DESC, uc.Id DESC;");
    }

    public int Create(CreateUsedCarRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var session = new DbSession(_factory);
        var repositories = RepositoryCatalog.For(session);
        var receivedAt = snapshot.IsReceived ? DateTime.UtcNow : (DateTime?)null;

        var usedCarId = session.Connection.ExecuteScalar<int>(
            @"INSERT INTO dbo.UsedCars
                (Barcode, SupplierId, CarModelId, ModelYear, PriceCurrency, Price, PriceBase, PriceCounter, LocationId, Location, Transportation, IsReceived, IsShipped, PartOutAmount, Shipping, Customs, Repairs, TotalBeforeShipping, GrandTotalBase, GrandTotalCounter, BaseCurrencyCode, CounterCurrencyCode, CounterRateToBase, ReceivedAt, CreatedByUserId)
              VALUES
                (@Barcode, @SupplierId, @CarModelId, @ModelYear, @PriceCurrency, @Price, @PriceBase, @PriceCounter, @LocationId, @Location, @Transportation, @IsReceived, @IsShipped, @PartOut, @Shipping, @Customs, @Repairs, @TotalBeforeShipping, @GrandTotalBase, @GrandTotalCounter, @BaseCurrencyCode, @CounterCurrencyCode, @CounterRateToBase, @ReceivedAt, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                snapshot.Barcode,
                snapshot.SupplierId,
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
                snapshot.Repairs,
                snapshot.TotalBeforeShipping,
                snapshot.GrandTotalBase,
                snapshot.GrandTotalCounter,
                snapshot.BaseCurrencyCode,
                snapshot.CounterCurrencyCode,
                snapshot.CounterRateToBase,
                ReceivedAt = receivedAt,
                UserId = userId
            },
            session.Transaction);

        session.Connection.Execute(
            @"UPDATE dbo.UsedCars
              SET Barcode = @Barcode
              WHERE Id = @Id
                AND (Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'');",
            new { Id = usedCarId, Barcode = $"UC-{usedCarId}" },
            session.Transaction);

        if (snapshot.IsReceived && receivedAt.HasValue)
        {
            SyncLinkedPurchaseDraft(repositories, usedCarId, snapshot, receivedAt.Value, userId);
        }

        SyncReceiveJournal(session, repositories, usedCarId, snapshot, receivedAt, userId);

        session.Commit();
        return usedCarId;
    }

    public void Update(int id, CreateUsedCarRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var session = new DbSession(_factory);
        var repositories = RepositoryCatalog.For(session);
        var existing = GetExistingState(session, id);
        if (existing == null)
        {
            throw new NotFoundException("Used car not found.");
        }

        EnsurePostedPurchaseCanBeUpdated(repositories.Purchases.UsedCarPurchases, existing, snapshot, id);

        var receivedAt = snapshot.IsReceived
            ? existing.IsReceived
                ? existing.ReceivedAt ?? DateTime.UtcNow
                : DateTime.UtcNow
            : (DateTime?)null;

        var updated = session.Connection.Execute(
            @"UPDATE dbo.UsedCars
              SET Barcode = @Barcode,
                  SupplierId = @SupplierId,
                  CarModelId = @CarModelId,
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
                  PartOutAmount = @PartOut,
                  Shipping = @Shipping,
                  Customs = @Customs,
                  Repairs = @Repairs,
                  TotalBeforeShipping = @TotalBeforeShipping,
                  GrandTotalBase = @GrandTotalBase,
                  GrandTotalCounter = @GrandTotalCounter,
                  BaseCurrencyCode = @BaseCurrencyCode,
                  CounterCurrencyCode = @CounterCurrencyCode,
                  CounterRateToBase = @CounterRateToBase,
                  ReceivedAt = @ReceivedAt,
                  ModifiedAt = @Now,
                  ModifiedByUserId = @UserId
              WHERE Id = @Id",
            new
            {
                Id = id,
                snapshot.Barcode,
                snapshot.SupplierId,
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
                snapshot.Repairs,
                snapshot.TotalBeforeShipping,
                snapshot.GrandTotalBase,
                snapshot.GrandTotalCounter,
                snapshot.BaseCurrencyCode,
                snapshot.CounterCurrencyCode,
                snapshot.CounterRateToBase,
                ReceivedAt = receivedAt,
                UserId = userId,
                Now = DateTime.UtcNow
            },
            session.Transaction);

        if (updated == 0)
        {
            throw new NotFoundException("Used car not found.");
        }

        if (snapshot.IsReceived && receivedAt.HasValue)
        {
            SyncLinkedPurchaseDraft(repositories, id, snapshot, receivedAt.Value, userId);
        }
        else
        {
            repositories.Purchases.UsedCarPurchases.DeleteDraftsByUsedCarId(id);
        }

        SyncReceiveJournal(session, repositories, id, snapshot, receivedAt, userId);

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory);
        var repositories = RepositoryCatalog.For(session);
        repositories.Accounting.Journal.DeleteEntriesByReference(UsedCarReferenceType, id);

        if (repositories.Purchases.UsedCarPurchases.HasPostedPurchase(id))
        {
            throw new ValidationException("This used car already has a posted purchase and cannot be deleted.");
        }

        repositories.Purchases.UsedCarPurchases.DeleteDraftsByUsedCarId(id);

        var deleted = session.Connection.Execute(
            "DELETE FROM dbo.UsedCars WHERE Id = @Id",
            new { Id = id },
            session.Transaction);

        if (deleted == 0)
        {
            throw new NotFoundException("Used car not found.");
        }

        session.Commit();
    }

    private UsedCarSnapshot BuildSnapshot(CreateUsedCarRequest request)
    {
        ValidateRequest(request);

        var normalizedPriceCurrency = NormalizeCurrencyCode(request.PriceCurrency)
            ?? throw new ValidationException("Price currency is required.");

        using var conn = _factory.CreateConnection();
        var supplierExists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.Suppliers WHERE Id = @Id",
            new { Id = request.SupplierId });
        if (supplierExists == 0)
        {
            throw new ValidationException("Selected supplier was not found.");
        }

        var carDisplayName = conn.QuerySingleOrDefault<string>(
            @"SELECT TOP (1)
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
                     END
              FROM dbo.CarModels cm
              INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
              WHERE cm.Id = @Id
                AND cm.IsActive = 1;",
            new { Id = request.CarModelId });
        if (string.IsNullOrWhiteSpace(carDisplayName))
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

        var normalizedLocationName = selectedLocation.Name?.Trim() ?? string.Empty;
        var roundedPrice = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        var roundedPartOut = decimal.Round(request.PartOut, 2, MidpointRounding.AwayFromZero);
        var roundedShipping = decimal.Round(request.Shipping, 2, MidpointRounding.AwayFromZero);
        var roundedCustoms = decimal.Round(request.Customs, 2, MidpointRounding.AwayFromZero);
        var roundedRepairs = decimal.Round(request.Repairs, 2, MidpointRounding.AwayFromZero);

        var priceBase = decimal.Round(roundedPrice * priceToBaseRate, 2, MidpointRounding.AwayFromZero);
        var priceCounter = counterToBaseRate > 0
            ? decimal.Round(priceBase / counterToBaseRate, 2, MidpointRounding.AwayFromZero)
            : priceBase;
        var transportation = decimal.Round(selectedLocation.ShippingFees * locationToCounterRate, 2, MidpointRounding.AwayFromZero);
        var totalBeforeShipping = decimal.Round(priceCounter + transportation, 2, MidpointRounding.AwayFromZero);
        var expensesCounterTotal = transportation + roundedPartOut + roundedShipping + roundedCustoms + roundedRepairs;
        var grandTotalCounter = decimal.Round(priceCounter + expensesCounterTotal, 2, MidpointRounding.AwayFromZero);
        var grandTotalBase = decimal.Round(priceBase + (expensesCounterTotal * counterToBaseRate), 2, MidpointRounding.AwayFromZero);

        return new UsedCarSnapshot
        {
            Barcode = NormalizeOptional(request.Barcode),
            SupplierId = request.SupplierId,
            CarModelId = request.CarModelId,
            ModelYear = request.ModelYear,
            CarDisplayName = $"{carDisplayName.Trim()} {request.ModelYear}",
            PriceCurrency = normalizedPriceCurrency,
            Price = roundedPrice,
            PriceBase = priceBase,
            PriceCounter = priceCounter,
            LocationId = selectedLocation.LocationID,
            Location = normalizedLocationName,
            Transportation = transportation,
            IsReceived = request.IsReceived,
            IsShipped = request.IsShipped,
            PartOut = roundedPartOut,
            Shipping = roundedShipping,
            Customs = roundedCustoms,
            Repairs = roundedRepairs,
            TotalBeforeShipping = totalBeforeShipping,
            GrandTotalBase = grandTotalBase,
            GrandTotalCounter = grandTotalCounter,
            BaseCurrencyCode = baseCurrencyCode,
            CounterCurrencyCode = counterCurrencyCode,
            CounterRateToBase = decimal.Round(counterToBaseRate, 8, MidpointRounding.AwayFromZero)
        };
    }

    private static void ValidateRequest(CreateUsedCarRequest request)
    {
        if (request.SupplierId <= 0)
        {
            throw new ValidationException("Supplier is required.");
        }

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

        if (request.PartOut < 0 || request.Shipping < 0 || request.Customs < 0 || request.Repairs < 0)
        {
            throw new ValidationException("Expense values cannot be negative.");
        }

        if (request.IsReceived && request.Customs <= 0)
        {
            throw new ValidationException("Customs should be different than 0 when the car is marked as received.");
        }
    }

    private void SyncReceiveJournal(
        DbSession session,
        RepositoryCatalog repositories,
        int usedCarId,
        UsedCarSnapshot snapshot,
        DateTime? receivedAt,
        int userId)
    {
        repositories.Accounting.Journal.DeleteEntriesByReference(UsedCarReferenceType, usedCarId);

        if (!snapshot.IsReceived || !receivedAt.HasValue)
        {
            return;
        }

        if (repositories.Purchases.UsedCarPurchases.HasPostedPurchase(usedCarId))
        {
            return;
        }

        var postingAccounts = repositories.Accounting.PostingSettings.GetAll()
            .Where(item => item.AccountId > 0)
            .ToDictionary(item => item.SettingKey, item => item.AccountId, StringComparer.OrdinalIgnoreCase);

        var linePlans = BuildReceiveJournalLinePlans(snapshot, postingAccounts);
        if (linePlans.Count == 0)
        {
            return;
        }

        AdjustLinePlansToGrandTotal(snapshot.GrandTotalBase, linePlans);
        var supplierAccountId = ResolveUsedCarSupplierAccountId(session, postingAccounts, snapshot.SupplierId);
        var createdAt = DateTime.UtcNow;
        var lines = linePlans
            .Select(plan => new JournalLine
            {
                AccountId = plan.AccountId,
                Debit = plan.BaseAmount,
                Credit = 0m,
                CurrencyCode = plan.CurrencyCode,
                OriginalAmount = plan.OriginalAmount,
                RateToBase = plan.RateToBase,
                CounterAmount = plan.CounterAmount,
                BaseCurrencyCode = snapshot.BaseCurrencyCode,
                CounterCurrencyCode = snapshot.CounterCurrencyCode,
                CreatedAt = createdAt,
                CreatedByUserId = userId
            })
            .ToList();

        lines.Add(new JournalLine
        {
            AccountId = supplierAccountId,
            Debit = 0m,
            Credit = snapshot.GrandTotalBase,
            CurrencyCode = snapshot.CounterCurrencyCode,
            OriginalAmount = snapshot.GrandTotalCounter > 0m ? snapshot.GrandTotalCounter : snapshot.GrandTotalBase,
            RateToBase = snapshot.CounterRateToBase > 0m
                ? decimal.Round(snapshot.CounterRateToBase, 8, MidpointRounding.AwayFromZero)
                : 1m,
            CounterAmount = snapshot.GrandTotalCounter > 0m ? snapshot.GrandTotalCounter : snapshot.GrandTotalBase,
            BaseCurrencyCode = snapshot.BaseCurrencyCode,
            CounterCurrencyCode = snapshot.CounterCurrencyCode,
            CreatedAt = createdAt,
            CreatedByUserId = userId
        });

        var totalDebit = decimal.Round(lines.Sum(line => line.Debit), 4, MidpointRounding.AwayFromZero);
        var totalCredit = decimal.Round(lines.Sum(line => line.Credit), 4, MidpointRounding.AwayFromZero);
        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException("Used-car receive journal entry is not balanced.");
        }

        var entry = new JournalEntry
        {
            EntryDate = receivedAt.Value,
            ReferenceType = UsedCarReferenceType,
            ReferenceId = usedCarId,
            Description = AccountingJournalDescriptionFormatter.FormatUsedCarReceipt(snapshot.CarDisplayName),
            CreatedAt = createdAt,
            CreatedByUserId = userId
        };

        var entryId = repositories.Accounting.Journal.InsertEntry(entry);
        repositories.Accounting.Journal.InsertLines(entryId, lines);
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

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ExistingUsedCarState? GetExistingState(DbSession session, int id)
        => session.Connection.QuerySingleOrDefault<ExistingUsedCarState>(
            @"SELECT Id,
                     SupplierId,
                     PriceCurrency,
                     Price,
                     PriceBase,
                     PriceCounter,
                     LocationId,
                     Transportation,
                     PartOutAmount AS PartOut,
                     Shipping,
                     Customs,
                     Repairs,
                     TotalBeforeShipping,
                     GrandTotalBase,
                     GrandTotalCounter,
                     BaseCurrencyCode,
                     CounterCurrencyCode,
                     CounterRateToBase,
                     IsReceived,
                     ReceivedAt
              FROM dbo.UsedCars
              WHERE Id = @Id;",
            new { Id = id },
            session.Transaction);

    private int ResolveUsedCarSupplierAccountId(
        DbSession session,
        IReadOnlyDictionary<string, int> postingAccounts,
        int supplierId)
    {
        var supplierAccountId = new SuppliersRepository(session).GetAccountId(supplierId);
        if (supplierAccountId is > 0)
        {
            return supplierAccountId.Value;
        }

        if (postingAccounts.TryGetValue(AccountingSettingKeys.PurchaseOffset, out var configuredOffsetAccountId)
            && configuredOffsetAccountId > 0)
        {
            return configuredOffsetAccountId;
        }

        var fallbackAccountId = _accountingSettingsProvider.GetSnapshot().PurchaseOffsetAccountId;
        if (fallbackAccountId > 0)
        {
            return fallbackAccountId;
        }

        throw new ValidationException("Configure the purchase offset account or link an account to the supplier before receiving used cars.");
    }

    private void SyncLinkedPurchaseDraft(
        RepositoryCatalog repositories,
        int usedCarId,
        UsedCarSnapshot snapshot,
        DateTime receivedAt,
        int userId)
    {
        var usedCarPurchasesRepository = repositories.Purchases.UsedCarPurchases;
        var existingDraft = usedCarPurchasesRepository.GetDraftByUsedCarId(usedCarId);
        if (existingDraft == null && usedCarPurchasesRepository.HasPostedPurchase(usedCarId))
        {
            return;
        }

        var postingAccounts = repositories.Accounting.PostingSettings.GetAll()
            .Where(item => item.AccountId > 0)
            .ToDictionary(item => item.SettingKey, item => item.AccountId, StringComparer.OrdinalIgnoreCase);

        var linePlans = BuildReceiveJournalLinePlans(snapshot, postingAccounts);
        if (linePlans.Count == 0)
        {
            throw new ValidationException("Used car receive posting requires at least one non-zero posting line.");
        }

        AdjustLinePlansToGrandTotal(snapshot.GrandTotalBase, linePlans);

        var createdAt = DateTime.UtcNow;
        var purchaseLines = linePlans
            .Select((plan, index) => new UsedCarPurchaseLine
            {
                DetailKey = plan.DetailKey,
                Description = plan.Description,
                Amount = plan.OriginalAmount,
                CurrencyCode = plan.CurrencyCode,
                RateToBase = plan.RateToBase,
                BaseAmount = plan.BaseAmount,
                CounterAmount = plan.CounterAmount,
                AccountId = plan.AccountId,
                SortOrder = index + 1,
                CreatedAt = createdAt,
                CreatedByUserId = userId
            })
            .ToList();

        var totalBaseAmount = decimal.Round(purchaseLines.Sum(line => line.BaseAmount), 4, MidpointRounding.AwayFromZero);
        var totalCounterAmount = decimal.Round(purchaseLines.Sum(line => line.CounterAmount), 4, MidpointRounding.AwayFromZero);
        var paidAmount = existingDraft?.PaidAmount ?? 0m;
        var paidCounterAmount = existingDraft?.PaidCounterAmount ?? 0m;
        var purchase = new UsedCarPurchase
        {
            PurchaseNumber = existingDraft?.PurchaseNumber ?? new UtcInvoiceNumberGenerator(_factory).NextUsedCarPurchaseNumber(),
            UsedCarId = usedCarId,
            SupplierId = snapshot.SupplierId,
            PurchaseDate = existingDraft?.PurchaseDate ?? receivedAt,
            BaseCurrencyCode = snapshot.BaseCurrencyCode,
            CounterCurrencyCode = snapshot.CounterCurrencyCode,
            TotalBaseAmount = totalBaseAmount,
            TotalCounterAmount = totalCounterAmount,
            PaidAmount = paidAmount,
            PaidCounterAmount = paidCounterAmount,
            PaymentStatus = ResolvePaymentStatus(totalBaseAmount, paidAmount),
            PostingStatus = "Draft",
            Notes = existingDraft?.Notes ?? string.Empty,
            CreatedAt = createdAt,
            CreatedByUserId = userId,
            Lines = purchaseLines
        };

        if (existingDraft == null)
        {
            var purchaseId = usedCarPurchasesRepository.Insert(purchase);
            usedCarPurchasesRepository.InsertLines(purchaseId, purchaseLines);
            return;
        }

        if (!usedCarPurchasesRepository.Update(existingDraft.Id, purchase))
        {
            throw new ValidationException("The linked used-car purchase draft could not be synchronized.");
        }

        usedCarPurchasesRepository.ReplaceLines(existingDraft.Id, purchaseLines);
    }

    private static List<UsedCarJournalLinePlan> BuildReceiveJournalLinePlans(
        UsedCarSnapshot snapshot,
        IReadOnlyDictionary<string, int> postingAccounts)
    {
        var linePlans = new List<UsedCarJournalLinePlan>();

        AddJournalLinePlan(
            linePlans,
            AccountingSettingKeys.UsedCarPrice,
            "Vehicle Price",
            ResolveRequiredPostingAccountId(postingAccounts, AccountingSettingKeys.UsedCarPrice, "Used Car Price"),
            snapshot.PriceBase,
            snapshot.PriceCurrency,
            snapshot.Price,
            snapshot.Price > 0m
                ? decimal.Round(snapshot.PriceBase / snapshot.Price, 8, MidpointRounding.AwayFromZero)
                : 1m,
            snapshot.PriceCounter > 0m ? snapshot.PriceCounter : snapshot.PriceBase);

        AddCounterJournalLinePlan(
            linePlans,
            AccountingSettingKeys.UsedCarTransportation,
            "Transportation",
            ResolveRequiredPostingAccountId(postingAccounts, AccountingSettingKeys.UsedCarTransportation, "Used Car Transportation"),
            snapshot.Transportation,
            snapshot.CounterRateToBase,
            snapshot.CounterCurrencyCode);

        AddCounterJournalLinePlan(
            linePlans,
            AccountingSettingKeys.UsedCarPartOut,
            "Part-Out",
            ResolveRequiredPostingAccountId(postingAccounts, AccountingSettingKeys.UsedCarPartOut, "Used Car Part-Out"),
            snapshot.PartOut,
            snapshot.CounterRateToBase,
            snapshot.CounterCurrencyCode);

        AddCounterJournalLinePlan(
            linePlans,
            AccountingSettingKeys.UsedCarShipping,
            "Shipping",
            ResolveRequiredPostingAccountId(postingAccounts, AccountingSettingKeys.UsedCarShipping, "Used Car Shipping"),
            snapshot.Shipping,
            snapshot.CounterRateToBase,
            snapshot.CounterCurrencyCode);

        AddCounterJournalLinePlan(
            linePlans,
            AccountingSettingKeys.UsedCarCustoms,
            "Customs",
            ResolveRequiredPostingAccountId(postingAccounts, AccountingSettingKeys.UsedCarCustoms, "Used Car Customs"),
            snapshot.Customs,
            snapshot.CounterRateToBase,
            snapshot.CounterCurrencyCode);

        AddCounterJournalLinePlan(
            linePlans,
            AccountingSettingKeys.UsedCarRepairs,
            "Repairs",
            ResolveRequiredPostingAccountId(postingAccounts, AccountingSettingKeys.UsedCarRepairs, "Used Car Repairs"),
            snapshot.Repairs,
            snapshot.CounterRateToBase,
            snapshot.CounterCurrencyCode);

        return linePlans;
    }

    private static void AddJournalLinePlan(
        ICollection<UsedCarJournalLinePlan> linePlans,
        string detailKey,
        string description,
        int accountId,
        decimal baseAmount,
        string currencyCode,
        decimal originalAmount,
        decimal rateToBase,
        decimal counterAmount)
    {
        if (baseAmount <= 0m)
        {
            return;
        }

        linePlans.Add(new UsedCarJournalLinePlan
        {
            DetailKey = detailKey,
            Description = description,
            AccountId = accountId,
            BaseAmount = decimal.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
            CurrencyCode = currencyCode,
            OriginalAmount = decimal.Round(originalAmount > 0m ? originalAmount : baseAmount, 2, MidpointRounding.AwayFromZero),
            RateToBase = rateToBase > 0m
                ? decimal.Round(rateToBase, 8, MidpointRounding.AwayFromZero)
                : 1m,
            CounterAmount = decimal.Round(counterAmount > 0m ? counterAmount : baseAmount, 2, MidpointRounding.AwayFromZero)
        });
    }

    private static void AddCounterJournalLinePlan(
        ICollection<UsedCarJournalLinePlan> linePlans,
        string detailKey,
        string description,
        int accountId,
        decimal counterAmount,
        decimal counterRateToBase,
        string counterCurrencyCode)
    {
        if (counterAmount <= 0m)
        {
            return;
        }

        AddJournalLinePlan(
            linePlans,
            detailKey,
            description,
            accountId,
            decimal.Round(counterAmount * counterRateToBase, 2, MidpointRounding.AwayFromZero),
            counterCurrencyCode,
            counterAmount,
            counterRateToBase,
            counterAmount);
    }

    private static void AdjustLinePlansToGrandTotal(decimal expectedGrandTotalBase, IList<UsedCarJournalLinePlan> linePlans)
    {
        if (linePlans.Count == 0)
        {
            return;
        }

        var plannedCreditTotal = decimal.Round(linePlans.Sum(line => line.BaseAmount), 2, MidpointRounding.AwayFromZero);
        var difference = decimal.Round(expectedGrandTotalBase - plannedCreditTotal, 2, MidpointRounding.AwayFromZero);
        if (difference == 0m)
        {
            return;
        }

        linePlans[^1].BaseAmount = decimal.Round(linePlans[^1].BaseAmount + difference, 2, MidpointRounding.AwayFromZero);
        if (linePlans[^1].BaseAmount <= 0m)
        {
            throw new InvalidOperationException("Used car receive journal entry could not be balanced.");
        }
    }

    private static void EnsurePostedPurchaseCanBeUpdated(
        IUsedCarPurchasesRepository usedCarPurchasesRepository,
        ExistingUsedCarState existing,
        UsedCarSnapshot snapshot,
        int usedCarId)
    {
        if (!usedCarPurchasesRepository.HasPostedPurchase(usedCarId))
        {
            return;
        }

        if (!snapshot.IsReceived)
        {
            throw new ValidationException("This used car already has a posted purchase and cannot be marked as not received.");
        }

        if (HasPurchaseAffectingChanges(existing, snapshot))
        {
            throw new ValidationException("This used car already has a posted purchase. Change the linked purchase through accounting instead of editing the used-car amounts here.");
        }
    }

    private static bool HasPurchaseAffectingChanges(ExistingUsedCarState existing, UsedCarSnapshot snapshot)
    {
        return existing.SupplierId != snapshot.SupplierId
            || !string.Equals(NormalizeCurrencyCode(existing.PriceCurrency), snapshot.PriceCurrency, StringComparison.OrdinalIgnoreCase)
            || decimal.Round(existing.Price, 2, MidpointRounding.AwayFromZero) != snapshot.Price
            || decimal.Round(existing.PriceBase, 2, MidpointRounding.AwayFromZero) != snapshot.PriceBase
            || decimal.Round(existing.PriceCounter, 2, MidpointRounding.AwayFromZero) != snapshot.PriceCounter
            || existing.LocationId != snapshot.LocationId
            || decimal.Round(existing.Transportation, 2, MidpointRounding.AwayFromZero) != snapshot.Transportation
            || decimal.Round(existing.PartOut, 2, MidpointRounding.AwayFromZero) != snapshot.PartOut
            || decimal.Round(existing.Shipping, 2, MidpointRounding.AwayFromZero) != snapshot.Shipping
            || decimal.Round(existing.Customs, 2, MidpointRounding.AwayFromZero) != snapshot.Customs
            || decimal.Round(existing.Repairs, 2, MidpointRounding.AwayFromZero) != snapshot.Repairs
            || decimal.Round(existing.TotalBeforeShipping, 2, MidpointRounding.AwayFromZero) != snapshot.TotalBeforeShipping
            || decimal.Round(existing.GrandTotalBase, 2, MidpointRounding.AwayFromZero) != snapshot.GrandTotalBase
            || decimal.Round(existing.GrandTotalCounter, 2, MidpointRounding.AwayFromZero) != snapshot.GrandTotalCounter
            || !string.Equals(NormalizeCurrencyCode(existing.BaseCurrencyCode), snapshot.BaseCurrencyCode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(NormalizeCurrencyCode(existing.CounterCurrencyCode), snapshot.CounterCurrencyCode, StringComparison.OrdinalIgnoreCase)
            || decimal.Round(existing.CounterRateToBase, 8, MidpointRounding.AwayFromZero) != snapshot.CounterRateToBase;
    }

    private static string ResolvePaymentStatus(decimal totalBaseAmount, decimal paidAmount)
    {
        if (paidAmount <= 0m)
        {
            return "Unpaid";
        }

        if (paidAmount >= totalBaseAmount)
        {
            return "Paid";
        }

        return "PartiallyPaid";
    }

    private static int ResolveRequiredPostingAccountId(
        IReadOnlyDictionary<string, int> postingAccounts,
        string settingKey,
        string label)
    {
        if (!postingAccounts.TryGetValue(settingKey, out var accountId) || accountId <= 0)
        {
            throw new ValidationException($"Configure the posting account for '{label}' before receiving used cars.");
        }

        return accountId;
    }
}
