using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using SpareParts.Domain.Growth;
using SpareParts.Domain.Transactions;

namespace SpareParts.Infrastructure.Services;

public sealed class GrowthIntelligenceService
{
    private readonly ISqlConnectionFactory _factory;

    public GrowthIntelligenceService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public GrowthBriefingDto GetBriefing()
    {
        using var connection = _factory.CreateConnection();
        var donorCars = LoadDonorCars(connection);
        var teardownQueue = BuildTeardownQueue(donorCars);
        var buyingRadar = LoadBuyingRadar(connection);
        var duplicates = LoadDuplicateGroups(connection);
        var moneyActions = LoadMoneyActions(connection, donorCars, buyingRadar);

        return new GrowthBriefingDto
        {
            GeneratedAt = DateTime.UtcNow,
            Currency = "USD",
            UnlockableValue = moneyActions.Sum(action => action.EstimatedValue),
            MoneyActions = moneyActions,
            DonorCars = donorCars,
            TeardownQueue = teardownQueue,
            DuplicateGroups = duplicates,
            BuyingRadar = buyingRadar
        };
    }

    public AuctionProfitSimulationDto SimulateAuction(AuctionProfitSimulationRequest request)
    {
        if (request.AuctionPrice <= 0m)
        {
            throw new ValidationException("Enter a positive auction price.");
        }

        using var connection = _factory.CreateConnection();
        var car = request.CarModelId is int carModelId
            ? connection.QuerySingleOrDefault<string>(
                """
SELECT CONCAT(ISNULL(cb.Name, N''), N' ', ISNULL(cm.Name, N''), CASE WHEN NULLIF(cm.BodyType, N'') IS NULL THEN N'' ELSE N' (' + cm.BodyType + N')' END)
FROM dbo.CarModels cm
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE cm.Id = @CarModelId;
""",
                new { CarModelId = carModelId })
            : null;
        var history = LoadAuctionHistory(connection, request.CarModelId);
        var costs = request.Transportation + request.Customs + request.Shipping + request.Repairs + request.PartOutCost;
        var loadedCost = request.AuctionPrice + costs;
        var historicalRecovery = history.ComparableCars > 0 ? history.AverageRecovery : 0m;
        var estimatedRecovery = historicalRecovery > 0m
            ? historicalRecovery
            : decimal.Round(loadedCost * 1.35m, 2, MidpointRounding.AwayFromZero);
        var desiredProfit = request.DesiredProfit > 0m
            ? request.DesiredProfit
            : decimal.Round(estimatedRecovery * 0.18m, 2, MidpointRounding.AwayFromZero);
        var maximumBid = Math.Max(0m, estimatedRecovery - costs - desiredProfit);
        var estimatedProfit = estimatedRecovery - loadedCost;

        return new AuctionProfitSimulationDto
        {
            Car = string.IsNullOrWhiteSpace(car) ? "Donor car" : car.Trim(),
            LoadedCost = decimal.Round(loadedCost, 2, MidpointRounding.AwayFromZero),
            EstimatedRecovery = decimal.Round(estimatedRecovery, 2, MidpointRounding.AwayFromZero),
            EstimatedProfit = decimal.Round(estimatedProfit, 2, MidpointRounding.AwayFromZero),
            MaximumBid = decimal.Round(maximumBid, 2, MidpointRounding.AwayFromZero),
            HistoricalAverageRecovery = decimal.Round(historicalRecovery, 2, MidpointRounding.AwayFromZero),
            ComparableCars = history.ComparableCars,
            Confidence = history.ComparableCars >= 3 ? "High" : history.ComparableCars > 0 ? "Medium" : "Starter",
            Notes =
            [
                history.ComparableCars > 0
                    ? $"Estimate uses {history.ComparableCars} comparable donor car record(s)."
                    : "No comparable donor cars were found, so the starter estimate uses a 35% recovery buffer.",
                $"Non-auction costs total {costs:N2}.",
                $"Maximum bid protects a target profit of {desiredProfit:N2}.",
                request.ModelYear is int modelYear ? $"Candidate model year: {modelYear}." : "Add a model year when comparing auction listings."
            ]
        };
    }

    public VoiceQuoteDto BuildVoiceQuote(VoiceQuoteRequest request)
    {
        var transcript = (request.Transcript ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(transcript))
        {
            throw new ValidationException("Speak or type the customer request first.");
        }

        var tokens = VisualPartSearchService.ExtractSearchTokens(transcript);
        using var connection = _factory.CreateConnection();
        var candidates = LoadQuoteCandidates(connection);
        var matches = candidates
            .Select(candidate => ScoreQuoteCandidate(candidate, tokens))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenByDescending(match => match.AvailableQuantity)
            .ThenBy(match => match.PartName)
            .Take(6)
            .ToList();
        var best = matches.FirstOrDefault();
        var customerName = string.IsNullOrWhiteSpace(request.CustomerName) ? "there" : request.CustomerName.Trim();
        var draft = best == null
            ? $"Hello {customerName}, we recorded your request: \"{transcript}\". We are checking stock and will reply with the best match."
            : $"Hello {customerName}, we found {best.PartName} ({best.InternalCode}) for your request. Available: {best.AvailableQuantity:N0}. Price: {best.Currency} {best.SalePrice:N2}. Reply to reserve it.";

        return new VoiceQuoteDto
        {
            Transcript = transcript,
            SearchText = string.Join(' ', tokens),
            VehicleHint = ExtractVehicleHint(transcript),
            DraftMessage = draft,
            Matches = matches
        };
    }

    private static List<MoneyActionDto> LoadMoneyActions(
        IDbConnection connection,
        IReadOnlyList<DonorCarTreasureDto> donorCars,
        IReadOnlyList<BuyingRadarItemDto> buyingRadar)
    {
        var actions = connection.Query<MoneyActionDto>(
            """
WITH StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
)
SELECT TOP (5)
       [Key] = CONCAT(N'request-', pr.Id),
       Kind = N'Waiting customer',
       Tone = N'Opportunity',
       Title = CONCAT(N'Call ', pr.CustomerName, N' about ', COALESCE(NULLIF(p.Name, N''), pr.RequestedPartName)),
       Detail = CONCAT(N'Request #', pr.Id, N' is matched and available. Reserve it before the customer goes elsewhere.'),
       EstimatedValue = p.SalePrice * CASE WHEN pr.Quantity < stock.AvailableQuantity THEN pr.Quantity ELSE stock.AvailableQuantity END,
       Currency = ISNULL(NULLIF(p.Currency, N''), N'USD'),
       TargetView = N'part-requests',
       PartId = p.Id,
       PartRequestId = pr.Id,
       UsedCarId = p.UsedCarId
FROM dbo.PartRequests pr
INNER JOIN dbo.Parts p ON p.Id = pr.PartId
INNER JOIN StockByPart stock ON stock.PartId = p.Id
WHERE pr.Status IN (N'Open', N'Contacted')
  AND stock.AvailableQuantity > 0
ORDER BY EstimatedValue DESC, pr.CreatedAt;
""").ToList();

        actions.AddRange(connection.Query<MoneyActionDto>(
            """
WITH StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
)
SELECT TOP (4)
       [Key] = CONCAT(N'price-', p.Id),
       Kind = N'Pricing',
       Tone = N'Warning',
       Title = CONCAT(N'Price ', p.Name),
       Detail = CONCAT(N'', stock.AvailableQuantity, N' unit(s) are sitting in stock without a counter-ready sale price.'),
       EstimatedValue = stock.AvailableQuantity * COALESCE(NULLIF(p.RecommendedPrice, 0), NULLIF(p.EstimatedMarketPrice, 0), NULLIF(p.AveragePrice, 0), NULLIF(p.MinimumSellPrice, 0), NULLIF(p.CostPrice, 0) * 1.35, 0),
       Currency = ISNULL(NULLIF(p.Currency, N''), N'USD'),
       TargetView = N'stock-arrival',
       PartId = p.Id,
       PartRequestId = NULL,
       UsedCarId = p.UsedCarId
FROM dbo.Parts p
INNER JOIN StockByPart stock ON stock.PartId = p.Id
WHERE p.IsActive = 1
  AND stock.AvailableQuantity > 0
  AND ISNULL(p.SalePrice, 0) <= 0
ORDER BY EstimatedValue DESC, p.Name;
"""));

        actions.AddRange(connection.Query<MoneyActionDto>(
            """
WITH SalesByPart AS
(
    SELECT ti.PartId,
           LastSoldAt = MAX(t.TransactionDate)
    FROM dbo.TransactionItems ti
    INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
    WHERE ti.PartId IS NOT NULL
      AND ISNULL(t.IsReturn, 0) = 0
    GROUP BY ti.PartId
),
StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
)
SELECT TOP (3)
       [Key] = CONCAT(N'dormant-', p.Id),
       Kind = N'Dead stock',
       Tone = N'Warning',
       Title = CONCAT(N'Revive ', p.Name),
       Detail = CONCAT(N'Dormant stock worth ', stock.AvailableQuantity * p.SalePrice, N' has not sold recently. Add it to a WhatsApp clearance bundle.'),
       EstimatedValue = stock.AvailableQuantity * p.SalePrice,
       Currency = ISNULL(NULLIF(p.Currency, N''), N'USD'),
       TargetView = N'dead-stock',
       PartId = p.Id,
       PartRequestId = NULL,
       UsedCarId = p.UsedCarId
FROM dbo.Parts p
INNER JOIN StockByPart stock ON stock.PartId = p.Id
LEFT JOIN SalesByPart sales ON sales.PartId = p.Id
WHERE p.IsActive = 1
  AND stock.AvailableQuantity > 0
  AND p.SalePrice > 0
  AND (sales.LastSoldAt IS NULL OR sales.LastSoldAt < DATEADD(DAY, -120, GETDATE()))
ORDER BY EstimatedValue DESC, p.Name;
""",
            new { SaleType = TransactionTypeKeys.Sale }));

        var donorCar = donorCars
            .Where(car => car.BreakEvenGap > 0m && car.ProjectedStockRevenue > 0m)
            .OrderByDescending(car => Math.Min(car.BreakEvenGap, car.ProjectedStockRevenue))
            .FirstOrDefault();
        if (donorCar != null)
        {
            actions.Add(new MoneyActionDto
            {
                Key = $"donor-{donorCar.UsedCarId}",
                Kind = "Donor car",
                Tone = "Opportunity",
                Title = $"Push {donorCar.Car} toward break-even",
                Detail = $"{donorCar.RecoveryPercent}% recovered. Work the highest-value linked parts next.",
                EstimatedValue = Math.Min(donorCar.BreakEvenGap, donorCar.ProjectedStockRevenue),
                TargetView = "growth-lab",
                UsedCarId = donorCar.UsedCarId
            });
        }

        var buy = buyingRadar.FirstOrDefault(item => item.WaitingCustomers > 0);
        if (buy != null)
        {
            actions.Add(new MoneyActionDto
            {
                Key = $"buy-{buy.PartId}",
                Kind = "Buying radar",
                Tone = "Opportunity",
                Title = $"Restock {buy.PartName}",
                Detail = $"{buy.WaitingCustomers} waiting customer(s) and {buy.AvailableQuantity:N0} available unit(s).",
                EstimatedValue = buy.EstimatedRevenue,
                Currency = buy.Currency,
                TargetView = "growth-lab",
                PartId = buy.PartId
            });
        }

        return actions
            .GroupBy(action => action.Key)
            .Select(group => group.First())
            .OrderByDescending(action => action.EstimatedValue)
            .Take(12)
            .ToList();
    }

    private static List<DonorCarTreasureDto> LoadDonorCars(IDbConnection connection)
    {
        var cars = connection.Query<DonorCarRow>(
            """
SELECT uc.Id AS UsedCarId,
       COALESCE(NULLIF(uc.Barcode, N''), CONCAT(N'UC-', uc.Id)) AS Barcode,
       CONCAT(ISNULL(cb.Name, N''), N' ', ISNULL(cm.Name, N''), CASE WHEN NULLIF(cm.BodyType, N'') IS NULL THEN N'' ELSE N' (' + cm.BodyType + N')' END) AS Car,
       uc.ModelYear,
       LoadedCost = CASE WHEN ISNULL(uc.GrandTotalBase, 0) > 0 THEN uc.GrandTotalBase ELSE ISNULL(uc.PriceBase, 0) END,
       RecoveredValue = ISNULL(sold.RecoveredValue, 0)
FROM dbo.UsedCars uc
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
OUTER APPLY
(
    SELECT RecoveredValue = SUM(CASE WHEN ISNULL(t.IsReturn, 0) = 1 THEN -ISNULL(ti.BaseAmount, ti.LineTotal) ELSE ISNULL(ti.BaseAmount, ti.LineTotal) END)
    FROM dbo.Parts p
    INNER JOIN dbo.TransactionItems ti ON ti.PartId = p.Id
    INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
    WHERE p.UsedCarId = uc.Id
) sold
WHERE uc.IsReceived = 1
ORDER BY uc.Id DESC;
""",
            new { SaleType = TransactionTypeKeys.Sale })
            .ToList();
        var partRows = connection.Query<DonorPartRow>(
            """
WITH StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
),
WaitingByPart AS
(
    SELECT PartId,
           WaitingCustomers = COUNT(1)
    FROM dbo.PartRequests
    WHERE PartId IS NOT NULL
      AND Status IN (N'Open', N'Contacted', N'Reserved')
    GROUP BY PartId
)
SELECT p.UsedCarId,
       p.Id AS PartId,
       p.InternalCode,
       p.Name AS PartName,
       p.OEMNumber AS OemNumber,
       ISNULL(c.Name, N'Other') AS CategoryName,
       ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity,
       ISNULL(waiting.WaitingCustomers, 0) AS WaitingCustomers,
       ISNULL(p.SalePrice, 0) AS SalePrice,
       SuggestedPrice = COALESCE(NULLIF(p.RecommendedPrice, 0), NULLIF(p.SalePrice, 0), NULLIF(p.EstimatedMarketPrice, 0), NULLIF(p.AveragePrice, 0), NULLIF(p.MinimumSellPrice, 0), NULLIF(p.CostPrice, 0) * 1.35, 0)
FROM dbo.Parts p
LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
LEFT JOIN StockByPart stock ON stock.PartId = p.Id
LEFT JOIN WaitingByPart waiting ON waiting.PartId = p.Id
WHERE p.IsActive = 1
  AND p.UsedCarId IS NOT NULL;
""").ToList();
        var partsByCar = partRows.ToLookup(part => part.UsedCarId);

        return cars.Select(car =>
        {
            var parts = partsByCar[car.UsedCarId]
                .Select(ToDonorPartOpportunity)
                .OrderByDescending(part => part.PriorityScore)
                .ThenByDescending(part => part.OpportunityValue)
                .ToList();
            var projectedRevenue = parts.Sum(part => part.OpportunityValue);
            var gap = Math.Max(car.LoadedCost - car.RecoveredValue, 0m);
            return new DonorCarTreasureDto
            {
                UsedCarId = car.UsedCarId,
                Barcode = car.Barcode,
                Car = car.Car.Trim(),
                ModelYear = car.ModelYear,
                LoadedCost = car.LoadedCost,
                RecoveredValue = car.RecoveredValue,
                BreakEvenGap = gap,
                ProjectedStockRevenue = projectedRevenue,
                RecoveryPercent = car.LoadedCost <= 0m
                    ? 0
                    : Math.Clamp((int)Math.Round(car.RecoveredValue / car.LoadedCost * 100m), 0, 999),
                Hotspots = parts
                    .GroupBy(part => part.Zone)
                    .Select(group => new DonorCarHotspotDto
                    {
                        Zone = group.Key,
                        PartCount = group.Count(),
                        WaitingCustomers = group.Sum(part => part.WaitingCustomers),
                        OpportunityValue = group.Sum(part => part.OpportunityValue)
                    })
                    .OrderByDescending(hotspot => hotspot.WaitingCustomers)
                    .ThenByDescending(hotspot => hotspot.OpportunityValue)
                    .ToList(),
                Parts = parts
            };
        })
        .OrderByDescending(car => car.BreakEvenGap > 0m)
        .ThenByDescending(car => Math.Min(car.BreakEvenGap, car.ProjectedStockRevenue))
        .ToList();
    }

    private static DonorCarPartOpportunityDto ToDonorPartOpportunity(DonorPartRow row)
    {
        var zone = ClassifyZone(row.CategoryName, row.PartName);
        var quantity = Math.Max(0, row.AvailableQuantity);
        var opportunityValue = quantity * row.SuggestedPrice;
        var priority = Math.Min(100,
            (row.WaitingCustomers * 24)
            + (quantity > 0 ? 14 : 0)
            + (row.SalePrice <= 0m ? 18 : 0)
            + (int)Math.Min(35m, opportunityValue / 100m));
        var action = row.WaitingCustomers > 0
            ? "Contact waiting customers"
            : row.SalePrice <= 0m
                ? "Inspect and price"
                : quantity > 0
                    ? "Photograph and list"
                    : "Review sold-out demand";

        return new DonorCarPartOpportunityDto
        {
            PartId = row.PartId,
            InternalCode = row.InternalCode,
            PartName = row.PartName,
            OemNumber = row.OemNumber,
            CategoryName = row.CategoryName,
            Zone = zone,
            AvailableQuantity = quantity,
            WaitingCustomers = row.WaitingCustomers,
            SalePrice = row.SalePrice,
            SuggestedPrice = row.SuggestedPrice,
            OpportunityValue = opportunityValue,
            PriorityScore = priority,
            Action = action
        };
    }

    private static List<TeardownQueueItemDto> BuildTeardownQueue(IReadOnlyList<DonorCarTreasureDto> donorCars)
    {
        var queue = donorCars
            .SelectMany(car => car.Parts
                .Where(part => part.AvailableQuantity > 0 || part.WaitingCustomers > 0)
                .Take(5)
                .Select(part => new TeardownQueueItemDto
                {
                    UsedCarId = car.UsedCarId,
                    Car = $"{car.ModelYear} {car.Car}".Trim(),
                    PartId = part.PartId,
                    PartName = part.PartName,
                    Zone = part.Zone,
                    Action = part.Action,
                    Reason = part.WaitingCustomers > 0
                        ? $"{part.WaitingCustomers} customer(s) waiting"
                        : $"{part.AvailableQuantity} unit(s) ready to monetize",
                    PriorityScore = part.PriorityScore,
                    EstimatedValue = part.OpportunityValue
                }))
            .OrderByDescending(item => item.PriorityScore)
            .ThenByDescending(item => item.EstimatedValue)
            .Take(18)
            .ToList();

        foreach (var car in donorCars.Where(car => car.Parts.Count == 0).Take(3))
        {
            queue.Add(new TeardownQueueItemDto
            {
                UsedCarId = car.UsedCarId,
                Car = $"{car.ModelYear} {car.Car}".Trim(),
                PartName = "First teardown pass",
                Zone = "Whole car",
                Action = "Inspect and create linked parts",
                Reason = "No dismantled parts have been recorded yet.",
                PriorityScore = 88,
                EstimatedValue = car.BreakEvenGap
            });
        }

        return queue
            .OrderByDescending(item => item.PriorityScore)
            .ThenByDescending(item => item.EstimatedValue)
            .Take(18)
            .ToList();
    }

    private static List<DuplicatePartGroupDto> LoadDuplicateGroups(IDbConnection connection)
    {
        var candidates = connection.Query<DuplicateCandidateRow>(
            """
WITH StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
)
SELECT TOP (2500)
       p.Id AS PartId,
       p.InternalCode,
       p.Name AS PartName,
       p.OEMNumber AS OemNumber,
       ISNULL(c.Name, N'Other') AS CategoryName,
       ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity,
       ISNULL(p.SalePrice, 0) AS SalePrice
FROM dbo.Parts p
LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
LEFT JOIN StockByPart stock ON stock.PartId = p.Id
WHERE p.IsActive = 1
ORDER BY p.Name, p.Id;
""").ToList();

        return candidates
            .Select(candidate => new
            {
                Candidate = candidate,
                OemKey = NormalizeCode(candidate.OemNumber),
                NameKey = NormalizeText($"{candidate.CategoryName}|{candidate.PartName}")
            })
            .Select(candidate => new
            {
                candidate.Candidate,
                Key = candidate.OemKey.Length >= 4 ? $"OEM:{candidate.OemKey}" : $"NAME:{candidate.NameKey}",
                Reason = candidate.OemKey.Length >= 4 ? "Same normalized OEM number" : "Same normalized category and name"
            })
            .Where(candidate => !candidate.Key.EndsWith(':'))
            .GroupBy(candidate => new { candidate.Key, candidate.Reason })
            .Where(group => group.Count() > 1)
            .Select(group => new DuplicatePartGroupDto
            {
                MatchKey = group.Key.Key,
                Reason = group.Key.Reason,
                CandidateCount = group.Count(),
                CombinedStock = group.Sum(item => item.Candidate.AvailableQuantity),
                Candidates = group
                    .Select(item => new DuplicatePartCandidateDto
                    {
                        PartId = item.Candidate.PartId,
                        InternalCode = item.Candidate.InternalCode,
                        PartName = item.Candidate.PartName,
                        OemNumber = item.Candidate.OemNumber,
                        CategoryName = item.Candidate.CategoryName,
                        AvailableQuantity = item.Candidate.AvailableQuantity,
                        SalePrice = item.Candidate.SalePrice
                    })
                    .OrderBy(candidate => candidate.InternalCode)
                    .ToList()
            })
            .OrderByDescending(group => group.CandidateCount)
            .ThenByDescending(group => group.CombinedStock)
            .Take(20)
            .ToList();
    }

    private static List<BuyingRadarItemDto> LoadBuyingRadar(IDbConnection connection)
    {
        var rows = connection.Query<BuyingRadarRow>(
            """
WITH StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
),
SalesByPart AS
(
    SELECT ti.PartId,
           SoldQuantityLast90 = SUM(ABS(ISNULL(ti.Quantity, 0)))
    FROM dbo.TransactionItems ti
    INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
    WHERE ti.PartId IS NOT NULL
      AND ISNULL(t.IsReturn, 0) = 0
      AND t.TransactionDate >= DATEADD(DAY, -90, GETDATE())
    GROUP BY ti.PartId
),
WaitingByPart AS
(
    SELECT PartId,
           WaitingCustomers = COUNT(1)
    FROM dbo.PartRequests
    WHERE PartId IS NOT NULL
      AND Status IN (N'Open', N'Contacted', N'Reserved')
    GROUP BY PartId
)
SELECT p.Id AS PartId,
       p.InternalCode,
       p.Name AS PartName,
       p.OEMNumber AS OemNumber,
       ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
       ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity,
       ISNULL(sales.SoldQuantityLast90, 0) AS SoldQuantityLast90,
       ISNULL(waiting.WaitingCustomers, 0) AS WaitingCustomers,
       ISNULL(p.MinStock, 0) AS MinStock,
       ISNULL(p.SalePrice, 0) AS SalePrice
FROM dbo.Parts p
LEFT JOIN StockByPart stock ON stock.PartId = p.Id
LEFT JOIN SalesByPart sales ON sales.PartId = p.Id
LEFT JOIN WaitingByPart waiting ON waiting.PartId = p.Id
WHERE p.IsActive = 1
  AND
  (
      ISNULL(waiting.WaitingCustomers, 0) > 0
      OR ISNULL(stock.AvailableQuantity, 0) <= ISNULL(p.MinStock, 0)
      OR ISNULL(sales.SoldQuantityLast90, 0) > ISNULL(stock.AvailableQuantity, 0)
  );
""",
            new { SaleType = TransactionTypeKeys.Sale })
            .ToList();

        return rows.Select(row =>
        {
            var monthlyVelocity = (int)Math.Ceiling(row.SoldQuantityLast90 / 3m);
            var demandTarget = Math.Max(row.MinStock, monthlyVelocity + row.WaitingCustomers);
            var suggested = Math.Max(1, demandTarget - (int)Math.Floor(row.AvailableQuantity));
            var urgency = Math.Min(100,
                row.WaitingCustomers * 28
                + (row.AvailableQuantity <= 0 ? 28 : 0)
                + Math.Min(26, monthlyVelocity * 3)
                + (row.AvailableQuantity <= row.MinStock ? 12 : 0));
            var reasons = new List<string>();
            if (row.WaitingCustomers > 0) reasons.Add($"{row.WaitingCustomers} waiting");
            if (row.AvailableQuantity <= row.MinStock) reasons.Add($"below min stock {row.MinStock}");
            if (monthlyVelocity > 0) reasons.Add($"{monthlyVelocity}/month velocity");

            return new BuyingRadarItemDto
            {
                PartId = row.PartId,
                InternalCode = row.InternalCode,
                PartName = row.PartName,
                OemNumber = row.OemNumber,
                Currency = row.Currency,
                AvailableQuantity = row.AvailableQuantity,
                SoldQuantityLast90 = row.SoldQuantityLast90,
                WaitingCustomers = row.WaitingCustomers,
                MinStock = row.MinStock,
                SuggestedQuantity = suggested,
                EstimatedRevenue = suggested * row.SalePrice,
                UrgencyScore = urgency,
                Reason = reasons.Count == 0 ? "Stock review" : string.Join(" / ", reasons)
            };
        })
        .OrderByDescending(item => item.UrgencyScore)
        .ThenByDescending(item => item.EstimatedRevenue)
        .Take(24)
        .ToList();
    }

    private static AuctionHistoryRow LoadAuctionHistory(IDbConnection connection, int? carModelId)
        => connection.QuerySingle<AuctionHistoryRow>(
            """
SELECT ComparableCars = COUNT(1),
       AverageRecovery = ISNULL(AVG(CAST(ISNULL(recovery.RecoveredValue, 0) + ISNULL(stock.ProjectedStockRevenue, 0) AS DECIMAL(19, 4))), 0)
FROM dbo.UsedCars uc
OUTER APPLY
(
    SELECT RecoveredValue = SUM(CASE WHEN ISNULL(t.IsReturn, 0) = 1 THEN -ISNULL(ti.BaseAmount, ti.LineTotal) ELSE ISNULL(ti.BaseAmount, ti.LineTotal) END)
    FROM dbo.Parts p
    INNER JOIN dbo.TransactionItems ti ON ti.PartId = p.Id
    INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleType
    WHERE p.UsedCarId = uc.Id
) recovery
OUTER APPLY
(
    SELECT ProjectedStockRevenue = SUM(
        (ISNULL(s.Quantity, 0) - ISNULL(s.ReservedQuantity, 0))
        * COALESCE(NULLIF(p.RecommendedPrice, 0), NULLIF(p.SalePrice, 0), NULLIF(p.EstimatedMarketPrice, 0), NULLIF(p.AveragePrice, 0), 0))
    FROM dbo.Parts p
    LEFT JOIN dbo.Stock s ON s.PartId = p.Id
    WHERE p.UsedCarId = uc.Id
) stock
WHERE (@CarModelId IS NULL OR uc.CarModelId = @CarModelId)
  AND uc.IsReceived = 1;
""",
            new { CarModelId = carModelId, SaleType = TransactionTypeKeys.Sale });

    private static List<QuoteCandidateRow> LoadQuoteCandidates(IDbConnection connection)
        => connection.Query<QuoteCandidateRow>(
            """
WITH StockByPart AS
(
    SELECT PartId,
           AvailableQuantity = SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0))
    FROM dbo.Stock
    GROUP BY PartId
)
SELECT TOP (1500)
       p.Id AS PartId,
       p.InternalCode,
       p.Name AS PartName,
       p.OEMNumber AS OemNumber,
       ISNULL(b.Name, N'') AS BrandName,
       ISNULL(c.Name, N'') AS CategoryName,
       ISNULL(p.Notes, N'') AS Notes,
       ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
       ISNULL(p.SalePrice, 0) AS SalePrice,
       ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity
FROM dbo.Parts p
LEFT JOIN dbo.Brands b ON b.Id = p.BrandId
LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
LEFT JOIN StockByPart stock ON stock.PartId = p.Id
WHERE p.IsActive = 1
ORDER BY CASE WHEN ISNULL(stock.AvailableQuantity, 0) > 0 THEN 0 ELSE 1 END, p.Name;
""").ToList();

    private static VoiceQuoteMatchDto ScoreQuoteCandidate(QuoteCandidateRow candidate, IReadOnlyList<string> tokens)
    {
        var searchable = NormalizeText(string.Join(' ', candidate.InternalCode, candidate.PartName, candidate.OemNumber, candidate.BrandName, candidate.CategoryName, candidate.Notes));
        var score = 0;
        foreach (var token in tokens)
        {
            var normalized = NormalizeText(token);
            if (normalized.Length == 0) continue;
            if (string.Equals(NormalizeCode(candidate.InternalCode), NormalizeCode(token), StringComparison.Ordinal)
                || string.Equals(NormalizeCode(candidate.OemNumber), NormalizeCode(token), StringComparison.Ordinal))
            {
                score += 42;
            }
            else if (searchable.Contains(normalized, StringComparison.Ordinal))
            {
                score += 9;
            }
        }

        if (candidate.AvailableQuantity > 0) score += 3;
        return new VoiceQuoteMatchDto
        {
            PartId = candidate.PartId,
            InternalCode = candidate.InternalCode,
            PartName = candidate.PartName,
            OemNumber = candidate.OemNumber,
            Currency = candidate.Currency,
            SalePrice = candidate.SalePrice,
            AvailableQuantity = candidate.AvailableQuantity,
            Score = score
        };
    }

    private static string ExtractVehicleHint(string transcript)
    {
        var year = Regex.Match(transcript, @"\b(?:19|20)\d{2}\b").Value;
        var words = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var hint = string.Join(' ', words.Take(9));
        return string.IsNullOrWhiteSpace(year) ? hint : $"{year} / {hint}";
    }

    private static string ClassifyZone(string? category, string? partName)
    {
        var text = NormalizeText($"{category} {partName}");
        if (ContainsAny(text, "engine", "turbo", "injector", "coil", "alternator", "starter")) return "Engine bay";
        if (ContainsAny(text, "gearbox", "transmission", "clutch", "differential", "axle")) return "Drivetrain";
        if (ContainsAny(text, "headlight", "light", "bumper", "grille", "hood", "bonnet", "fender")) return "Front";
        if (ContainsAny(text, "taillight", "tail light", "trunk", "boot", "rear")) return "Rear";
        if (ContainsAny(text, "door", "mirror", "window", "glass", "seat", "dashboard", "console", "airbag")) return "Cabin and sides";
        if (ContainsAny(text, "wheel", "brake", "suspension", "shock", "strut", "arm")) return "Chassis";
        return "Other";
    }

    private static bool ContainsAny(string value, params string[] terms)
        => terms.Any(term => value.Contains(term, StringComparison.Ordinal));

    private static string NormalizeCode(string? value)
    {
        var builder = new StringBuilder();
        foreach (var character in value ?? string.Empty)
        {
            if (char.IsLetterOrDigit(character)) builder.Append(char.ToUpperInvariant(character));
        }
        return builder.ToString();
    }

    private static string NormalizeText(string? value)
    {
        var builder = new StringBuilder();
        var previousWasSpace = false;
        foreach (var character in (value ?? string.Empty).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }
        return builder.ToString().Trim();
    }

    private sealed class DonorCarRow
    {
        public int UsedCarId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Car { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public decimal LoadedCost { get; set; }
        public decimal RecoveredValue { get; set; }
    }

    private sealed class DonorPartRow
    {
        public int UsedCarId { get; set; }
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public int WaitingCustomers { get; set; }
        public decimal SalePrice { get; set; }
        public decimal SuggestedPrice { get; set; }
    }

    private sealed class DuplicateCandidateRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public decimal SalePrice { get; set; }
    }

    private sealed class BuyingRadarRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal AvailableQuantity { get; set; }
        public decimal SoldQuantityLast90 { get; set; }
        public int WaitingCustomers { get; set; }
        public int MinStock { get; set; }
        public decimal SalePrice { get; set; }
    }

    private sealed class AuctionHistoryRow
    {
        public int ComparableCars { get; set; }
        public decimal AverageRecovery { get; set; }
    }

    private sealed class QuoteCandidateRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string BrandName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Currency { get; set; } = "USD";
        public decimal SalePrice { get; set; }
        public decimal AvailableQuantity { get; set; }
    }
}
