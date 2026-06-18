using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Data.Repositories;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Interfaces.Repositories;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SpareParts.Infrastructure.Services;

public sealed class UsedCarsService
{
    private const string UsedCarReferenceType = "UsedCar";
    private const string UsedCarWholesaleSaleReferenceType = "UsedCarWholesaleSale";
    private const string UsedCarWholesalePaymentReferenceType = "UsedCarWholesalePayment";

    private readonly ISqlConnectionFactory _factory;
    private readonly CurrenciesService _currenciesService;
    private readonly AppConstantsService _appConstantsService;
    private readonly AccountingSettingsProvider _accountingSettingsProvider;
    private readonly ITenantContext _tenantContext;

    public UsedCarsService(
        ISqlConnectionFactory factory,
        CurrenciesService currenciesService,
        AppConstantsService appConstantsService,
        AccountingSettingsProvider accountingSettingsProvider,
        ITenantContext tenantContext)
    {
        _factory = factory;
        _currenciesService = currenciesService;
        _appConstantsService = appConstantsService;
        _accountingSettingsProvider = accountingSettingsProvider;
        _tenantContext = tenantContext;
    }

    public IEnumerable<UsedCarDto> GetAll()
    {
        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        var cars = conn.Query<UsedCarDto>(
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
                     uc.ExpectedSellThroughRate,
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
                     COALESCE(wholesale.SalePriceBase, sales.PartsSoldAmountBase) AS SalePriceBase,
                     wholesale.WholesaleSaleId,
                     wholesale.WholesaleSaleNumber,
                     CAST(CASE WHEN wholesale.WholesaleSaleId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsWholesaleSold,
                     COALESCE(wholesale.WholesaleBuyerName, N'') AS WholesaleBuyerName,
                     wholesale.WholesaleBuyerPhone,
                     wholesale.WholesaleSoldAt,
                     COALESCE(wholesale.WholesaleSaleCurrency, uc.BaseCurrencyCode) AS WholesaleSaleCurrency,
                     COALESCE(wholesale.WholesaleSalePrice, 0) AS WholesaleSalePrice,
                     COALESCE(wholesale.SalePriceBase, 0) AS WholesaleSalePriceBase,
                     COALESCE(wholesale.WholesaleSalePriceCounter, 0) AS WholesaleSalePriceCounter,
                     COALESCE(wholesale.WholesalePaymentStatus, N'') AS WholesalePaymentStatus,
                     CAST(COALESCE(wholesale.IsWholesaleForParts, 0) AS BIT) AS IsWholesaleForParts,
                     COALESCE(wholesale.WholesaleRepairTotalAmount, 0) AS WholesaleRepairTotalAmount,
                     COALESCE(wholesale.WholesaleRepairTotalBaseAmount, 0) AS WholesaleRepairTotalBaseAmount,
                     COALESCE(wholesale.WholesaleRepairTotalCounterAmount, 0) AS WholesaleRepairTotalCounterAmount,
                     CAST(COALESCE(wholesale.WholesaleSoldAsIsAcknowledged, 0) AS BIT) AS WholesaleSoldAsIsAcknowledged,
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
                         PartsRemovedValueBase = ISNULL(SUM(COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0)), 0)
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
                  SELECT TOP (1)
                         WholesaleSaleId = w.Id,
                         WholesaleSaleNumber = w.SaleNumber,
                         WholesaleBuyerName = COALESCE(NULLIF(LTRIM(RTRIM(c.Name)), N''), NULLIF(LTRIM(RTRIM(w.BuyerName)), N''), N'Wholesale buyer'),
                         WholesaleBuyerPhone = COALESCE(NULLIF(LTRIM(RTRIM(c.Phone)), N''), NULLIF(LTRIM(RTRIM(w.BuyerPhone)), N'')),
                         WholesaleSoldAt = w.SaleDate,
                         WholesaleSaleCurrency = w.CurrencyCode,
                         WholesaleSalePrice = w.SalePrice,
                         SalePriceBase = w.SalePriceBase,
                         WholesaleSalePriceCounter = w.SalePriceCounter,
                         WholesalePaymentStatus = w.PaymentStatus,
                         IsWholesaleForParts = w.IsForParts,
                         WholesaleRepairTotalAmount = w.RepairTotalAmount,
                         WholesaleRepairTotalBaseAmount = w.RepairTotalBaseAmount,
                         WholesaleRepairTotalCounterAmount = w.RepairTotalCounterAmount,
                         WholesaleSoldAsIsAcknowledged = w.SoldAsIsAcknowledged
                  FROM dbo.UsedCarWholesaleSales w
                  LEFT JOIN dbo.Customers c ON c.Id = w.CustomerId
                  WHERE w.UsedCarId = uc.Id
                  ORDER BY w.SaleDate DESC, w.Id DESC
              ) wholesale
              OUTER APPLY
              (
                  SELECT RemainingStockQuantity = ISNULL(SUM(CAST(st.Quantity AS DECIMAL(19, 4))), 0),
                         RemainingStockValueBase = ISNULL(SUM(CAST(st.Quantity AS DECIMAL(19, 4)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0)), 0)
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
                         FullCostBase = ROUND(CASE
                             WHEN ISNULL(uc.GrandTotalBase, 0) > 0 THEN uc.GrandTotalBase
                             ELSE ISNULL(uc.PriceBase, 0) + ((ISNULL(uc.Transportation, 0) + ISNULL(uc.PartOutAmount, 0) + ISNULL(uc.Shipping, 0) + ISNULL(uc.Customs, 0) + ISNULL(uc.Repairs, 0)) * rate.CounterRateToBase)
                         END, 2)
              ) cost
              CROSS APPLY
              (
                  SELECT NetProfitLossBase = CASE
                             WHEN wholesale.WholesaleSaleId IS NOT NULL THEN ROUND(ISNULL(wholesale.SalePriceBase, 0) - cost.FullCostBase - ISNULL(wholesale.WholesaleRepairTotalBaseAmount, 0), 2)
                             ELSE ROUND(ISNULL(sales.PartsSoldAmountBase, 0) - cost.FullCostBase, 2)
                         END
              ) profit
              WHERE (@TenantId = 0 OR uc.TenantId = @TenantId)
              ORDER BY cb.Name, cm.Name, uc.ModelYear DESC, uc.Id DESC;",
            new { TenantId = tenantId })
            .ToList();

        ApplyUsedCarInventoryValuation(conn, cars);
        return cars;
    }

    private void ApplyUsedCarInventoryValuation(System.Data.IDbConnection connection, IReadOnlyCollection<UsedCarDto> cars)
    {
        if (cars.Count == 0)
        {
            return;
        }

        var carIds = cars.Select(car => car.Id).ToArray();
        var partRows = connection.Query<UsedCarPartInventoryValueRow>(
            @"SELECT p.Id AS PartId,
                     p.UsedCarId,
                     p.Currency,
                     UnitCost = COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0),
                     Quantity = ISNULL(SUM(CAST(ISNULL(st.Quantity, 0) AS DECIMAL(19, 4))), 0)
              FROM dbo.Parts p
              LEFT JOIN dbo.Stock st ON st.PartId = p.Id
              WHERE p.UsedCarId IN @CarIds
              GROUP BY p.Id,
                       p.UsedCarId,
                       p.Currency,
                       p.AllocatedCost,
                       p.CostPrice,
                       p.AveragePrice;",
            new { CarIds = carIds })
            .ToList();

        if (partRows.Count == 0)
        {
            foreach (var car in cars)
            {
                car.PartsRemovedCount = 0;
                car.PartsRemovedValueBase = 0m;
                car.RemainingStockQuantity = 0m;
                car.RemainingStockValueBase = 0m;
                car.NetProfitLossBase = CalculateUsedCarProfitBase(car);
            }

            return;
        }

        var rates = _currenciesService.GetAll().ToList();
        var partsByCar = partRows.ToLookup(row => row.UsedCarId);
        foreach (var car in cars)
        {
            var rows = partsByCar[car.Id].ToList();
            car.PartsRemovedCount = rows.Count;
            car.PartsRemovedValueBase = decimal.Round(rows.Sum(row => row.UnitCost * ResolvePartRateToBase(row.Currency, car, rates)), 2, MidpointRounding.AwayFromZero);
            car.RemainingStockQuantity = rows.Sum(row => row.Quantity);
            car.RemainingStockValueBase = decimal.Round(rows.Sum(row => row.Quantity * row.UnitCost * ResolvePartRateToBase(row.Currency, car, rates)), 2, MidpointRounding.AwayFromZero);
            car.NetProfitLossBase = CalculateUsedCarProfitBase(car);
        }
    }

    private static decimal ResolvePartRateToBase(
        string? partCurrency,
        UsedCarDto car,
        IReadOnlyCollection<CurrencyRateDto> rates)
    {
        var baseCurrencyCode = NormalizeCurrencyCode(car.BaseCurrencyCode) ?? "USD";
        var counterCurrencyCode = NormalizeCurrencyCode(car.CounterCurrencyCode) ?? baseCurrencyCode;
        var currencyCode = NormalizeCurrencyCode(partCurrency) ?? counterCurrencyCode;
        var counterRateToBase = car.CounterRateToBase > 0m ? car.CounterRateToBase : 1m;
        var rateToBase = ResolveRateToBaseCurrency(
            currencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            counterRateToBase);

        return rateToBase > 0m ? rateToBase : 1m;
    }

    private static decimal CalculateUsedCarProfitBase(UsedCarDto car)
    {
        var profitBase = car.WholesaleSaleId is not null
            ? car.WholesaleSalePriceBase - car.FullCostBase - car.WholesaleRepairTotalBaseAmount
            : car.PartsSoldAmountBase - car.FullCostBase;

        return decimal.Round(profitBase, 2, MidpointRounding.AwayFromZero);
    }

    public int Create(CreateUsedCarRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var repositories = RepositoryCatalog.For(session);
        var receivedAt = snapshot.IsReceived ? DateTime.UtcNow : (DateTime?)null;

        var usedCarId = session.Connection.ExecuteScalar<int>(
            @"INSERT INTO dbo.UsedCars
                (Barcode, SupplierId, CarModelId, ModelYear, PriceCurrency, Price, PriceBase, PriceCounter, LocationId, Location, Transportation, IsReceived, IsShipped, PartOutAmount, Shipping, Customs, Repairs, TotalBeforeShipping, GrandTotalBase, GrandTotalCounter, ExpectedSellThroughRate, BaseCurrencyCode, CounterCurrencyCode, CounterRateToBase, ReceivedAt, CreatedByUserId, TenantId)
              VALUES
                (@Barcode, @SupplierId, @CarModelId, @ModelYear, @PriceCurrency, @Price, @PriceBase, @PriceCounter, @LocationId, @Location, @Transportation, @IsReceived, @IsShipped, @PartOut, @Shipping, @Customs, @Repairs, @TotalBeforeShipping, @GrandTotalBase, @GrandTotalCounter, @ExpectedSellThroughRate, @BaseCurrencyCode, @CounterCurrencyCode, @CounterRateToBase, @ReceivedAt, @UserId, @TenantId);
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
                snapshot.ExpectedSellThroughRate,
                snapshot.BaseCurrencyCode,
                snapshot.CounterCurrencyCode,
                snapshot.CounterRateToBase,
                ReceivedAt = receivedAt,
                UserId = userId,
                TenantId = session.TenantId > 0 ? (int?)session.TenantId : null
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
        UsedCarPartPricingAllocator.RepriceUsedCarParts(session, usedCarId, userId);

        session.Commit();
        return usedCarId;
    }

    public IEnumerable<UsedCarWholesaleSaleDto> GetWholesaleSales()
    {
        BackfillMissingWholesaleSaleJournalEntries();

        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        return conn.Query<UsedCarWholesaleSaleRecord>(
            @"SELECT w.Id,
                     w.SaleNumber,
                     w.UsedCarId,
                     UsedCar = CASE
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
                     END,
                     uc.Barcode,
                     w.CustomerId,
                     COALESCE(NULLIF(LTRIM(RTRIM(c.Name)), N''), NULLIF(LTRIM(RTRIM(w.BuyerName)), N''), N'Wholesale buyer') AS BuyerName,
                     COALESCE(NULLIF(LTRIM(RTRIM(c.Phone)), N''), NULLIF(LTRIM(RTRIM(w.BuyerPhone)), N'')) AS BuyerPhone,
                     w.SaleDate,
                     w.CurrencyCode,
                     w.SalePrice,
                     w.SaleRateToBase,
                     w.SalePriceBase,
                     w.CounterCurrencyCode,
                     w.CounterRateToBase,
                     w.SalePriceCounter,
                     w.PaidAmount,
                     w.PaidBaseAmount,
                     w.PaidCounterAmount,
                     w.PaymentStatus,
                     w.IsForParts,
                     w.RepairItemsJson,
                     w.RepairTotalAmount,
                     w.RepairTotalBaseAmount,
                     w.RepairTotalCounterAmount,
                     w.PaymentMethod,
                     w.Notes,
                     w.SoldAsIsAcknowledged,
                     w.CreatedAt,
                     w.CreatedByUserId
              FROM dbo.UsedCarWholesaleSales w
              INNER JOIN dbo.UsedCars uc ON uc.Id = w.UsedCarId
              INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
              INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
              LEFT JOIN dbo.Customers c ON c.Id = w.CustomerId
              WHERE (@TenantId = 0 OR uc.TenantId = @TenantId)
              ORDER BY w.SaleDate DESC, w.Id DESC;",
            new { TenantId = tenantId })
            .Select(MapWholesaleSale);
    }

    public CreateUsedCarWholesaleSaleResponse CreateWholesaleSale(
        int usedCarId,
        CreateUsedCarWholesaleSaleRequest request,
        int userId)
    {
        if (!request.SoldAsIsAcknowledged)
        {
            throw new ValidationException("The buyer must acknowledge the used car is sold as-is.");
        }

        if (request.SalePrice <= 0m)
        {
            throw new ValidationException("Sale price must be greater than zero.");
        }

        if (request.PaidAmount < 0m)
        {
            throw new ValidationException("Paid amount cannot be negative.");
        }

        var currencyCode = NormalizeCurrencyCode(request.CurrencyCode)
            ?? throw new ValidationException("Sale currency is required.");
        var saleDate = request.SaleDate == default ? DateTime.Today : request.SaleDate;
        var roundedSalePrice = decimal.Round(request.SalePrice, 4, MidpointRounding.AwayFromZero);
        var roundedPaidAmount = decimal.Round(request.PaidAmount, 4, MidpointRounding.AwayFromZero);
        var currencyContext = BuildSaleCurrencyContext(currencyCode);

        var salePriceBase = decimal.Round(roundedSalePrice * currencyContext.SaleRateToBase, 4, MidpointRounding.AwayFromZero);
        var salePriceCounter = ConvertBaseToCounter(salePriceBase, currencyContext.CounterRateToBase);
        var paidBaseAmount = decimal.Round(roundedPaidAmount * currencyContext.SaleRateToBase, 4, MidpointRounding.AwayFromZero);
        var paidCounterAmount = ConvertBaseToCounter(paidBaseAmount, currencyContext.CounterRateToBase);
        var paymentStatus = ResolvePaymentStatus(salePriceBase, paidBaseAmount);
        var repairItems = request.IsForParts ? new List<UsedCarWholesaleRepairItemDto>() : NormalizeRepairItems(request.RepairItems);
        var repairTotalAmount = decimal.Round(repairItems.Sum(item => item.Price), 4, MidpointRounding.AwayFromZero);
        var repairTotalBaseAmount = decimal.Round(repairTotalAmount * currencyContext.SaleRateToBase, 4, MidpointRounding.AwayFromZero);
        var repairTotalCounterAmount = ConvertBaseToCounter(repairTotalBaseAmount, currencyContext.CounterRateToBase);
        var repairItemsJson = JsonSerializer.Serialize(repairItems);

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var repositories = RepositoryCatalog.For(session);
        var usedCar = LoadWholesaleUsedCar(session, usedCarId)
            ?? throw new NotFoundException("Used car not found.");

        if (HasWholesaleSale(session, usedCarId))
        {
            throw new ValidationException("This used car is already sold through wholesale.");
        }

        var customer = request.CustomerId is > 0
            ? LoadWholesaleCustomer(session, request.CustomerId.Value)
                ?? throw new ValidationException("Selected customer was not found.")
            : null;

        var buyerName = NormalizeOptional(request.BuyerName) ?? customer?.Name;
        if (string.IsNullOrWhiteSpace(buyerName))
        {
            throw new ValidationException("Buyer name is required when no customer is selected.");
        }

        var buyerPhone = NormalizeOptional(request.BuyerPhone) ?? customer?.Phone;
        var saleNumber = session.Connection.ExecuteScalar<string>(
            @"DECLARE @SequenceValue INT = NEXT VALUE FOR dbo.UsedCarWholesaleSaleNumberSequence;
              SELECT CONCAT(N'UCW-', CONVERT(CHAR(8), SYSUTCDATETIME(), 112), N'-', RIGHT(N'00000000' + CAST(@SequenceValue AS NVARCHAR(20)), 8));",
            transaction: session.Transaction)
            ?? throw new InvalidOperationException("Could not generate used-car wholesale sale number.");

        var createdAt = DateTime.UtcNow;
        var saleId = session.Connection.ExecuteScalar<int>(
            @"INSERT INTO dbo.UsedCarWholesaleSales
                (SaleNumber, UsedCarId, CustomerId, BuyerName, BuyerPhone, SaleDate, CurrencyCode, SalePrice, SaleRateToBase, SalePriceBase, CounterCurrencyCode, CounterRateToBase, SalePriceCounter, PaidAmount, PaidBaseAmount, PaidCounterAmount, PaymentStatus, IsForParts, RepairItemsJson, RepairTotalAmount, RepairTotalBaseAmount, RepairTotalCounterAmount, PaymentMethod, Notes, SoldAsIsAcknowledged, CreatedAt, CreatedByUserId)
              VALUES
                (@SaleNumber, @UsedCarId, @CustomerId, @BuyerName, @BuyerPhone, @SaleDate, @CurrencyCode, @SalePrice, @SaleRateToBase, @SalePriceBase, @CounterCurrencyCode, @CounterRateToBase, @SalePriceCounter, @PaidAmount, @PaidBaseAmount, @PaidCounterAmount, @PaymentStatus, @IsForParts, @RepairItemsJson, @RepairTotalAmount, @RepairTotalBaseAmount, @RepairTotalCounterAmount, @PaymentMethod, @Notes, @SoldAsIsAcknowledged, @CreatedAt, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                SaleNumber = saleNumber,
                UsedCarId = usedCar.Id,
                CustomerId = customer?.Id,
                BuyerName = buyerName,
                BuyerPhone = buyerPhone,
                SaleDate = saleDate,
                CurrencyCode = currencyCode,
                SalePrice = roundedSalePrice,
                SaleRateToBase = currencyContext.SaleRateToBase,
                SalePriceBase = salePriceBase,
                CounterCurrencyCode = currencyContext.CounterCurrencyCode,
                CounterRateToBase = currencyContext.CounterRateToBase,
                SalePriceCounter = salePriceCounter,
                PaidAmount = roundedPaidAmount,
                PaidBaseAmount = paidBaseAmount,
                PaidCounterAmount = paidCounterAmount,
                PaymentStatus = paymentStatus,
                request.IsForParts,
                RepairItemsJson = repairItemsJson,
                RepairTotalAmount = repairTotalAmount,
                RepairTotalBaseAmount = repairTotalBaseAmount,
                RepairTotalCounterAmount = repairTotalCounterAmount,
                PaymentMethod = NormalizeOptional(request.PaymentMethod),
                Notes = NormalizeOptional(request.Notes),
                SoldAsIsAcknowledged = request.SoldAsIsAcknowledged,
                CreatedAt = createdAt,
                UserId = userId
            },
            session.Transaction);

        MarkUsedCarChangedByWholesaleSale(session, usedCar.Id, userId);
        CreateWholesaleSaleJournalEntries(
            session,
            repositories,
            usedCar,
            saleId,
            saleNumber,
            customer?.Id,
            saleDate,
            currencyCode,
            roundedSalePrice,
            salePriceBase,
            salePriceCounter,
            roundedPaidAmount,
            paidBaseAmount,
            paidCounterAmount,
            repairTotalBaseAmount,
            currencyContext,
            userId);

        session.Commit();
        return new CreateUsedCarWholesaleSaleResponse
        {
            SaleId = saleId,
            SaleNumber = saleNumber,
            UsedCarId = usedCar.Id,
            PaymentStatus = paymentStatus
        };
    }

    public void Update(int id, CreateUsedCarRequest request, int userId)
    {
        var snapshot = BuildSnapshot(request);

        using var session = new DbSession(_factory, _tenantContext.TenantId);
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
                  ExpectedSellThroughRate = @ExpectedSellThroughRate,
                  BaseCurrencyCode = @BaseCurrencyCode,
                  CounterCurrencyCode = @CounterCurrencyCode,
                  CounterRateToBase = @CounterRateToBase,
                  ReceivedAt = @ReceivedAt,
                  ModifiedAt = @Now,
                  ModifiedByUserId = @UserId
              WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)",
            new
            {
                Id = id,
                TenantId = session.TenantId,
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
                snapshot.ExpectedSellThroughRate,
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
        UsedCarPartPricingAllocator.RepriceUsedCarParts(session, id, userId);

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var repositories = RepositoryCatalog.For(session);

        if (GetExistingState(session, id) == null)
        {
            throw new NotFoundException("Used car not found.");
        }

        repositories.Accounting.Journal.DeleteEntriesByReference(UsedCarReferenceType, id);

        if (repositories.Purchases.UsedCarPurchases.HasPostedPurchase(id))
        {
            throw new ValidationException("This used car already has a posted purchase and cannot be deleted.");
        }

        repositories.Purchases.UsedCarPurchases.DeleteDraftsByUsedCarId(id);

        var deleted = session.Connection.Execute(
            "DELETE FROM dbo.UsedCars WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)",
            new { Id = id, TenantId = session.TenantId },
            session.Transaction);

        if (deleted == 0)
        {
            throw new NotFoundException("Used car not found.");
        }

        session.Commit();
    }

    public UsedCarListingPackageDto BuildListingPackage(int id)
    {
        var car = GetAll().FirstOrDefault(c => c.Id == id)
            ?? throw new NotFoundException($"Used car {id} not found.");

        using var conn = _factory.CreateConnection();
        var photoCount = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.usedcar_images WHERE UsedCarId = @UsedCarId",
            new { UsedCarId = id });

        var priceText = $"{car.PriceCurrency} {car.Price:N0}";
        var title = $"{car.Car} {car.ModelYear} - {priceText}";
        var location = string.IsNullOrWhiteSpace(car.Location) ? "available now" : $"located in {car.Location}";
        var hashtag = car.Car.Replace(" ", string.Empty);

        var description =
            $"{car.Car} {car.ModelYear}\n" +
            $"Price: {priceText}\n" +
            (string.IsNullOrWhiteSpace(car.Location) ? "" : $"Location: {car.Location}\n") +
            $"\nWell-maintained {car.Car} ({car.ModelYear}), {location}. " +
            "Contact us for more details, an inspection, or a test drive.\n\n" +
            $"#UsedCars #{hashtag} #{car.ModelYear} #ForSale";

        return new UsedCarListingPackageDto
        {
            UsedCarId = car.Id,
            Title = title,
            Description = description,
            PriceText = priceText,
            PhotoCount = photoCount,
            MarketplaceLinks =
            [
                new MarketplaceLinkDto { Name = "Facebook Marketplace", Url = "https://www.facebook.com/marketplace/create/vehicle" },
                new MarketplaceLinkDto { Name = "OLX", Url = "https://www.olx.com/" },
                new MarketplaceLinkDto { Name = "Dubizzle", Url = "https://www.dubizzle.com/" }
            ]
        };
    }

    private static UsedCarWholesaleSaleDto MapWholesaleSale(UsedCarWholesaleSaleRecord record)
    {
        return new UsedCarWholesaleSaleDto
        {
            Id = record.Id,
            SaleNumber = record.SaleNumber,
            UsedCarId = record.UsedCarId,
            UsedCar = record.UsedCar,
            Barcode = record.Barcode,
            CustomerId = record.CustomerId,
            BuyerName = record.BuyerName,
            BuyerPhone = record.BuyerPhone,
            SaleDate = record.SaleDate,
            CurrencyCode = record.CurrencyCode,
            SalePrice = record.SalePrice,
            SaleRateToBase = record.SaleRateToBase,
            SalePriceBase = record.SalePriceBase,
            CounterCurrencyCode = record.CounterCurrencyCode,
            CounterRateToBase = record.CounterRateToBase,
            SalePriceCounter = record.SalePriceCounter,
            PaidAmount = record.PaidAmount,
            PaidBaseAmount = record.PaidBaseAmount,
            PaidCounterAmount = record.PaidCounterAmount,
            PaymentStatus = record.PaymentStatus,
            IsForParts = record.IsForParts,
            RepairTotalAmount = record.RepairTotalAmount,
            RepairTotalBaseAmount = record.RepairTotalBaseAmount,
            RepairTotalCounterAmount = record.RepairTotalCounterAmount,
            RepairItems = DeserializeRepairItems(record.RepairItemsJson),
            PaymentMethod = record.PaymentMethod,
            Notes = record.Notes,
            SoldAsIsAcknowledged = record.SoldAsIsAcknowledged,
            CreatedAt = record.CreatedAt
        };
    }

    private static List<UsedCarWholesaleRepairItemDto> NormalizeRepairItems(IEnumerable<UsedCarWholesaleRepairItemDto>? items)
    {
        var normalizedItems = new List<UsedCarWholesaleRepairItemDto>();
        foreach (var item in items ?? Enumerable.Empty<UsedCarWholesaleRepairItemDto>())
        {
            var description = NormalizeOptional(item.Description);
            var price = decimal.Round(item.Price, 4, MidpointRounding.AwayFromZero);
            if (string.IsNullOrWhiteSpace(description) && price <= 0m)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ValidationException("Every repair item must have a description.");
            }

            if (price < 0m)
            {
                throw new ValidationException("Repair item prices cannot be negative.");
            }

            normalizedItems.Add(new UsedCarWholesaleRepairItemDto
            {
                Description = description,
                Price = price
            });
        }

        return normalizedItems;
    }

    private static List<UsedCarWholesaleRepairItemDto> DeserializeRepairItems(string? repairItemsJson)
    {
        if (string.IsNullOrWhiteSpace(repairItemsJson))
        {
            return new List<UsedCarWholesaleRepairItemDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<UsedCarWholesaleRepairItemDto>>(repairItemsJson)
                ?? new List<UsedCarWholesaleRepairItemDto>();
        }
        catch (JsonException)
        {
            return new List<UsedCarWholesaleRepairItemDto>();
        }
    }

    private UsedCarWholesaleSaleCurrencyContext BuildSaleCurrencyContext(string currencyCode)
    {
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

        var saleRateToBase = ResolveRateToBaseCurrency(
            currencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (saleRateToBase <= 0)
        {
            throw new ValidationException($"No conversion rate is configured for {currencyCode}.");
        }

        var counterRateToBase = ResolveRateToBaseCurrency(
            counterCurrencyCode,
            rates,
            baseCurrencyCode,
            counterCurrencyCode,
            defaultCounterRate);
        if (counterRateToBase <= 0)
        {
            counterRateToBase = defaultCounterRate > 0 ? defaultCounterRate : 1m;
        }

        return new UsedCarWholesaleSaleCurrencyContext(
            decimal.Round(saleRateToBase, 8, MidpointRounding.AwayFromZero),
            counterCurrencyCode,
            decimal.Round(counterRateToBase, 8, MidpointRounding.AwayFromZero));
    }

    private static decimal ConvertBaseToCounter(decimal baseAmount, decimal counterRateToBase)
    {
        if (baseAmount <= 0m)
        {
            return 0m;
        }

        var effectiveRate = counterRateToBase > 0m ? counterRateToBase : 1m;
        return decimal.Round(baseAmount / effectiveRate, 4, MidpointRounding.AwayFromZero);
    }

    private static UsedCarWholesaleLookup? LoadWholesaleUsedCar(DbSession session, int usedCarId)
        => session.Connection.QuerySingleOrDefault<UsedCarWholesaleLookup>(
            @"SELECT uc.Id,
                     uc.Barcode,
                     UsedCar = CASE
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
                     END,
                     uc.ModelYear,
                     uc.BaseCurrencyCode,
                     uc.CounterCurrencyCode,
                     CounterRateToBase = CASE WHEN ISNULL(uc.CounterRateToBase, 0) > 0 THEN uc.CounterRateToBase ELSE 1 END,
                     FullCostBase = ROUND(CASE
                         WHEN ISNULL(uc.GrandTotalBase, 0) > 0 THEN uc.GrandTotalBase
                         ELSE ISNULL(uc.PriceBase, 0) + ((ISNULL(uc.Transportation, 0) + ISNULL(uc.PartOutAmount, 0) + ISNULL(uc.Shipping, 0) + ISNULL(uc.Customs, 0) + ISNULL(uc.Repairs, 0)) * CASE WHEN ISNULL(uc.CounterRateToBase, 0) > 0 THEN uc.CounterRateToBase ELSE 1 END)
                     END, 4)
              FROM dbo.UsedCars uc
              INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
              INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
              WHERE uc.Id = @Id AND (@TenantId = 0 OR uc.TenantId = @TenantId);",
            new { Id = usedCarId, TenantId = session.TenantId },
            session.Transaction);

    private static bool HasWholesaleSale(DbSession session, int usedCarId)
        => session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCarWholesaleSales WITH (UPDLOCK, HOLDLOCK) WHERE UsedCarId = @UsedCarId;",
            new { UsedCarId = usedCarId },
            session.Transaction) > 0;

    private static UsedCarWholesaleCustomerLookup? LoadWholesaleCustomer(DbSession session, int customerId)
        => session.Connection.QuerySingleOrDefault<UsedCarWholesaleCustomerLookup>(
            @"SELECT Id,
                     Name,
                     Phone
              FROM dbo.Customers
              WHERE Id = @Id;",
            new { Id = customerId },
            session.Transaction);

    private static void MarkUsedCarChangedByWholesaleSale(DbSession session, int usedCarId, int? userId)
        => session.Connection.Execute(
            @"UPDATE dbo.UsedCars
              SET ModifiedAt = SYSUTCDATETIME(),
                  ModifiedByUserId = @UserId
              WHERE Id = @Id;",
            new { Id = usedCarId, UserId = userId },
            session.Transaction);

    private void CreateWholesaleSaleJournalEntries(
        DbSession session,
        RepositoryCatalog repositories,
        UsedCarWholesaleLookup usedCar,
        int saleId,
        string saleNumber,
        int? customerId,
        DateTime saleDate,
        string saleCurrencyCode,
        decimal salePrice,
        decimal salePriceBase,
        decimal salePriceCounter,
        decimal paidAmount,
        decimal paidBaseAmount,
        decimal paidCounterAmount,
        decimal repairTotalBaseAmount,
        UsedCarWholesaleSaleCurrencyContext saleCurrencyContext,
        int? userId)
    {
        var settings = _accountingSettingsProvider.GetSnapshot();
        ValidateWholesalePostingAccounts(settings);

        var customerAccountId = customerId is > 0
            ? new CustomersRepository(session).GetAccountId(customerId.Value)
            : null;
        var receivableOrCashAccountId = customerAccountId ?? settings.SalesCashAccountId;
        var createdAt = DateTime.UtcNow;
        var baseCurrencyCode = NormalizeCurrencyCode(usedCar.BaseCurrencyCode) ?? "USD";
        var counterCurrencyCode = NormalizeCurrencyCode(usedCar.CounterCurrencyCode) ?? saleCurrencyContext.CounterCurrencyCode;
        var counterRateToBase = usedCar.CounterRateToBase > 0m ? usedCar.CounterRateToBase : saleCurrencyContext.CounterRateToBase;
        var costBaseAmount = decimal.Round(
            Math.Max(0m, usedCar.FullCostBase) + Math.Max(0m, repairTotalBaseAmount),
            4,
            MidpointRounding.AwayFromZero);
        var costCounterAmount = ConvertBaseToCounter(costBaseAmount, counterRateToBase);

        var saleLines = new List<JournalLine>
        {
            CreateWholesaleJournalLine(
                receivableOrCashAccountId,
                salePriceBase,
                0m,
                saleCurrencyCode,
                salePrice,
                saleCurrencyContext.SaleRateToBase,
                salePriceCounter,
                baseCurrencyCode,
                counterCurrencyCode,
                createdAt,
                userId),
            CreateWholesaleJournalLine(
                settings.SalesRevenueAccountId,
                0m,
                salePriceBase,
                saleCurrencyCode,
                salePrice,
                saleCurrencyContext.SaleRateToBase,
                salePriceCounter,
                baseCurrencyCode,
                counterCurrencyCode,
                createdAt,
                userId)
        };

        if (costBaseAmount > 0m)
        {
            saleLines.Add(CreateWholesaleJournalLine(
                settings.CogsAccountId,
                costBaseAmount,
                0m,
                baseCurrencyCode,
                costBaseAmount,
                1m,
                costCounterAmount,
                baseCurrencyCode,
                counterCurrencyCode,
                createdAt,
                userId));
            saleLines.Add(CreateWholesaleJournalLine(
                settings.InventoryAccountId,
                0m,
                costBaseAmount,
                baseCurrencyCode,
                costBaseAmount,
                1m,
                costCounterAmount,
                baseCurrencyCode,
                counterCurrencyCode,
                createdAt,
                userId));
        }

        EnsureBalanced(saleLines, "Used-car wholesale sale journal entry is not balanced.");

        var saleEntryId = repositories.Accounting.Journal.InsertEntry(new JournalEntry
        {
            EntryDate = saleDate,
            ReferenceType = UsedCarWholesaleSaleReferenceType,
            ReferenceId = saleId,
            Description = FormatWholesaleJournalDescription("Used car wholesale sale", usedCar, saleNumber),
            CreatedAt = createdAt,
            CreatedByUserId = userId
        });
        repositories.Accounting.Journal.InsertLines(saleEntryId, saleLines);

        if (customerAccountId is not > 0 || paidBaseAmount <= 0m)
        {
            return;
        }

        var paymentLines = new List<JournalLine>
        {
            CreateWholesaleJournalLine(
                settings.SalesCashAccountId,
                paidBaseAmount,
                0m,
                saleCurrencyCode,
                paidAmount,
                saleCurrencyContext.SaleRateToBase,
                paidCounterAmount,
                baseCurrencyCode,
                counterCurrencyCode,
                createdAt,
                userId),
            CreateWholesaleJournalLine(
                customerAccountId.Value,
                0m,
                paidBaseAmount,
                saleCurrencyCode,
                paidAmount,
                saleCurrencyContext.SaleRateToBase,
                paidCounterAmount,
                baseCurrencyCode,
                counterCurrencyCode,
                createdAt,
                userId)
        };
        EnsureBalanced(paymentLines, "Used-car wholesale payment journal entry is not balanced.");

        var paymentEntryId = repositories.Accounting.Journal.InsertEntry(new JournalEntry
        {
            EntryDate = saleDate,
            ReferenceType = UsedCarWholesalePaymentReferenceType,
            ReferenceId = saleId,
            Description = FormatWholesaleJournalDescription("Used car wholesale payment", usedCar, saleNumber),
            CreatedAt = createdAt,
            CreatedByUserId = userId
        });
        repositories.Accounting.Journal.InsertLines(paymentEntryId, paymentLines);
    }

    private void BackfillMissingWholesaleSaleJournalEntries()
    {
        using var session = new DbSession(_factory);
        var repositories = RepositoryCatalog.For(session);
        var missingSales = session.Connection.Query<UsedCarWholesaleSaleRecord>(
            @"SELECT w.Id,
                     w.SaleNumber,
                     w.UsedCarId,
                     w.CustomerId,
                     w.SaleDate,
                     w.CurrencyCode,
                     w.SalePrice,
                     w.SaleRateToBase,
                     w.SalePriceBase,
                     w.CounterCurrencyCode,
                     w.CounterRateToBase,
                     w.SalePriceCounter,
                     w.PaidAmount,
                     w.PaidBaseAmount,
                     w.PaidCounterAmount,
                     w.RepairTotalBaseAmount,
                     w.CreatedByUserId
              FROM dbo.UsedCarWholesaleSales w
              WHERE NOT EXISTS
              (
                  SELECT 1
                  FROM dbo.JournalEntries je
                  WHERE je.ReferenceType = @ReferenceType
                    AND je.ReferenceId = w.Id
              )
              ORDER BY w.Id;",
            new { ReferenceType = UsedCarWholesaleSaleReferenceType },
            session.Transaction)
            .ToList();

        if (missingSales.Count == 0)
        {
            return;
        }

        foreach (var sale in missingSales)
        {
            var usedCar = LoadWholesaleUsedCar(session, sale.UsedCarId);
            if (usedCar == null)
            {
                continue;
            }

            CreateWholesaleSaleJournalEntries(
                session,
                repositories,
                usedCar,
                sale.Id,
                sale.SaleNumber,
                sale.CustomerId,
                sale.SaleDate,
                NormalizeCurrencyCode(sale.CurrencyCode) ?? usedCar.BaseCurrencyCode,
                sale.SalePrice,
                sale.SalePriceBase,
                sale.SalePriceCounter,
                sale.PaidAmount,
                sale.PaidBaseAmount,
                sale.PaidCounterAmount,
                sale.RepairTotalBaseAmount,
                new UsedCarWholesaleSaleCurrencyContext(
                    sale.SaleRateToBase > 0m ? sale.SaleRateToBase : 1m,
                    NormalizeCurrencyCode(sale.CounterCurrencyCode) ?? usedCar.CounterCurrencyCode,
                    sale.CounterRateToBase > 0m ? sale.CounterRateToBase : usedCar.CounterRateToBase),
                sale.CreatedByUserId);

            MarkUsedCarChangedByWholesaleSale(session, usedCar.Id, sale.CreatedByUserId);
        }

        session.Commit();
    }

    private static JournalLine CreateWholesaleJournalLine(
        int accountId,
        decimal debitBaseAmount,
        decimal creditBaseAmount,
        string currencyCode,
        decimal originalAmount,
        decimal rateToBase,
        decimal counterAmount,
        string baseCurrencyCode,
        string counterCurrencyCode,
        DateTime createdAt,
        int? userId)
        => new()
        {
            AccountId = accountId,
            Debit = decimal.Round(Math.Max(0m, debitBaseAmount), 4, MidpointRounding.AwayFromZero),
            Credit = decimal.Round(Math.Max(0m, creditBaseAmount), 4, MidpointRounding.AwayFromZero),
            CurrencyCode = NormalizeCurrencyCode(currencyCode) ?? baseCurrencyCode,
            OriginalAmount = decimal.Round(Math.Max(0m, originalAmount), 4, MidpointRounding.AwayFromZero),
            RateToBase = rateToBase > 0m ? decimal.Round(rateToBase, 8, MidpointRounding.AwayFromZero) : 1m,
            CounterAmount = decimal.Round(Math.Max(0m, counterAmount), 4, MidpointRounding.AwayFromZero),
            BaseCurrencyCode = baseCurrencyCode,
            CounterCurrencyCode = counterCurrencyCode,
            CreatedAt = createdAt,
            CreatedByUserId = userId
        };

    private static void EnsureBalanced(IReadOnlyCollection<JournalLine> lines, string message)
    {
        var totalDebit = decimal.Round(lines.Sum(line => line.Debit), 4, MidpointRounding.AwayFromZero);
        var totalCredit = decimal.Round(lines.Sum(line => line.Credit), 4, MidpointRounding.AwayFromZero);
        if (totalDebit != totalCredit)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateWholesalePostingAccounts(AccountingPostingSettingsSnapshot settings)
    {
        if (settings.SalesCashAccountId <= 0)
        {
            throw new ValidationException("Configure the sales cash account before recording used-car wholesale sales.");
        }

        if (settings.SalesRevenueAccountId <= 0)
        {
            throw new ValidationException("Configure the sales revenue account before recording used-car wholesale sales.");
        }

        if (settings.CogsAccountId <= 0)
        {
            throw new ValidationException("Configure the cost of goods sold account before recording used-car wholesale sales.");
        }

        if (settings.InventoryAccountId <= 0)
        {
            throw new ValidationException("Configure the inventory account before recording used-car wholesale sales.");
        }
    }

    private static string FormatWholesaleJournalDescription(
        string prefix,
        UsedCarWholesaleLookup usedCar,
        string saleNumber)
    {
        var carLabel = string.IsNullOrWhiteSpace(usedCar.UsedCar)
            ? usedCar.Barcode
            : $"{usedCar.UsedCar} {usedCar.ModelYear}".Trim();
        var description = string.IsNullOrWhiteSpace(carLabel)
            ? $"{prefix} - {saleNumber}"
            : $"{prefix} - {carLabel} ({saleNumber})";
        return description.Length <= 240 ? description : description[..240];
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
            ExpectedSellThroughRate = UsedVehiclePartPricingEngine.NormalizeExpectedSellThroughRate(request.ExpectedSellThroughRate),
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

        if (request.ExpectedSellThroughRate <= 0m || request.ExpectedSellThroughRate > 1m)
        {
            throw new ValidationException("Expected sell-through rate must be between 0 and 1.");
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
              WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, TenantId = session.TenantId },
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

    private sealed class UsedCarPartInventoryValueRow
    {
        public int PartId { get; set; }
        public int UsedCarId { get; set; }
        public string? Currency { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Quantity { get; set; }
    }

    private sealed record UsedCarWholesaleSaleCurrencyContext(
        decimal SaleRateToBase,
        string CounterCurrencyCode,
        decimal CounterRateToBase);

    private sealed class UsedCarWholesaleSaleRecord
    {
        public int Id { get; set; }
        public string SaleNumber { get; set; } = string.Empty;
        public int UsedCarId { get; set; }
        public string UsedCar { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public int? CustomerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerPhone { get; set; }
        public DateTime SaleDate { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal SalePrice { get; set; }
        public decimal SaleRateToBase { get; set; } = 1m;
        public decimal SalePriceBase { get; set; }
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal CounterRateToBase { get; set; } = 1m;
        public decimal SalePriceCounter { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal PaidBaseAmount { get; set; }
        public decimal PaidCounterAmount { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public bool IsForParts { get; set; }
        public string? RepairItemsJson { get; set; }
        public decimal RepairTotalAmount { get; set; }
        public decimal RepairTotalBaseAmount { get; set; }
        public decimal RepairTotalCounterAmount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public bool SoldAsIsAcknowledged { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
    }

    private sealed class UsedCarWholesaleLookup
    {
        public int Id { get; set; }
        public string? Barcode { get; set; }
        public string UsedCar { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public string BaseCurrencyCode { get; set; } = "USD";
        public string CounterCurrencyCode { get; set; } = "USD";
        public decimal CounterRateToBase { get; set; } = 1m;
        public decimal FullCostBase { get; set; }
    }

    private sealed class UsedCarWholesaleCustomerLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
