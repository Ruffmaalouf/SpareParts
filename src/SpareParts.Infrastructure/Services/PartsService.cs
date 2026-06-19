using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Pricing;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class PartsService
{
    private const int UsedCarPartInitialStockQuantity = 1;
    private const string UsedCarStockReferenceType = "UsedCar";

    private readonly ISqlConnectionFactory _factory;
    private readonly PartNotesAiService _partNotesAiService;
    private readonly IInventoryService _inventoryService;
    private readonly ITenantContext _tenantContext;
    private readonly ISubscriptionLimitService _subscriptionLimitService;

    public PartsService(
        ISqlConnectionFactory factory,
        PartNotesAiService partNotesAiService,
        IInventoryService inventoryService,
        ITenantContext tenantContext,
        ISubscriptionLimitService subscriptionLimitService)
    {
        _factory = factory;
        _partNotesAiService = partNotesAiService;
        _inventoryService = inventoryService;
        _tenantContext = tenantContext;
        _subscriptionLimitService = subscriptionLimitService;
    }

    public (IEnumerable<PartDto> Items, int TotalCount) GetAll(int page, int pageSize, int? usedCarId = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var offset = Math.Max(0, (page - 1) * pageSize);

        using var multi = session.Connection.QueryMultiple(
            """
WITH FilteredParts AS
(
    SELECT
        p.Id,
        p.InternalCode,
        p.Barcode,
        p.Name,
        p.OEMNumber,
        p.Condition,
        p.CategoryId,
        p.BrandId,
        p.CostPrice,
        p.SalePrice,
        p.AveragePrice,
        p.EstimatedMarketPrice,
        p.CostAllocationPercent,
        p.AllocatedCost,
        p.MinimumSellPrice,
        p.FastSalePrice,
        p.WholesalePrice,
        p.RecommendedPrice,
        p.PricingStatus,
        p.PricingCalculatedAt,
        p.Currency,
        p.MinStock,
        p.Notes,
        p.UsedCarId,
        p.IsActive
    FROM dbo.Parts p
    WHERE p.IsActive = 1
      AND (@UsedCarId IS NULL OR p.UsedCarId = @UsedCarId)
      AND (@TenantId = 0 OR p.TenantId = @TenantId)
),
PagedParts AS
(
    SELECT *
    FROM FilteredParts
    ORDER BY Name, Id
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
)
SELECT
    p.Id,
    p.InternalCode,
    p.Barcode,
    p.Name,
    p.OEMNumber,
    p.Condition,
    p.CategoryId,
    p.BrandId,
    p.CostPrice,
    p.SalePrice,
    p.AveragePrice,
    p.EstimatedMarketPrice,
    p.CostAllocationPercent,
    p.AllocatedCost,
    p.MinimumSellPrice,
    p.FastSalePrice,
    p.WholesalePrice,
    p.RecommendedPrice,
    p.PricingStatus,
    p.PricingCalculatedAt,
    p.Currency,
    p.MinStock,
    StockQuantity = ISNULL(stock.Quantity, 0),
    ReservedQuantity = ISNULL(stock.ReservedQuantity, 0),
    AvailableQuantity = ISNULL(stock.Quantity - stock.ReservedQuantity, 0),
    p.Notes,
    p.UsedCarId,
    p.IsActive
FROM PagedParts p
LEFT JOIN
(
    SELECT
        s.PartId,
        Quantity = SUM(ISNULL(s.Quantity, 0)),
        ReservedQuantity = SUM(ISNULL(s.ReservedQuantity, 0))
    FROM dbo.Stock s
    INNER JOIN PagedParts paged ON paged.Id = s.PartId
    GROUP BY s.PartId
) stock ON stock.PartId = p.Id
ORDER BY p.Name, p.Id;

SELECT COUNT(1)
FROM dbo.Parts p
WHERE p.IsActive = 1
  AND (@UsedCarId IS NULL OR p.UsedCarId = @UsedCarId)
  AND (@TenantId = 0 OR p.TenantId = @TenantId);
""",
            new
            {
                UsedCarId = usedCarId,
                Offset = offset,
                PageSize = pageSize,
                TenantId = _tenantContext.TenantId
            },
            session.Transaction);

        var items = multi.Read<PartDto>().ToList();
        var totalCount = multi.ReadFirst<int>();
        return (items, totalCount);
    }

    public IReadOnlyList<PartStockDto> GetStockByWarehouse(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        EnsurePartExists(session, partId);

        var rows = session.Connection.Query<PartStockDto>(
            """
SELECT
    @PartId AS PartId,
    w.Id AS WarehouseId,
    w.Name AS WarehouseName,
    Quantity = ISNULL(SUM(s.Quantity), 0),
    ReservedQuantity = ISNULL(SUM(s.ReservedQuantity), 0),
    AvailableQuantity = ISNULL(SUM(s.Quantity - s.ReservedQuantity), 0)
FROM dbo.Warehouses w
LEFT JOIN dbo.Stock s ON s.WarehouseId = w.Id
                      AND s.PartId = @PartId
GROUP BY w.Id, w.Name, w.IsMain
ORDER BY w.IsMain DESC, w.Name;
""",
            new { PartId = partId },
            session.Transaction)
            .ToList();

        session.Commit();
        return rows;
    }

    public DeadStockReportDto GetDeadStock(int minDormantDays = 90, int take = 25)
    {
        minDormantDays = Math.Clamp(minDormantDays, 30, 720);
        take = Math.Clamp(take, 1, 100);

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var today = DateTime.Today;
        var recentCutoff = today.AddDays(-90);

        var rows = session.Connection.Query<DeadStockQueryRow>(
            """
WITH SalesByPart AS
(
    SELECT
        ti.PartId,
        MAX(t.TransactionDate) AS LastSoldAt,
        SUM(ABS(ISNULL(ti.Quantity, 0))) AS SoldQuantityAllTime,
        SUM(CASE WHEN t.TransactionDate >= @RecentCutoff THEN ABS(ISNULL(ti.Quantity, 0)) ELSE 0 END) AS SoldQuantityLast90
    FROM dbo.TransactionItems ti
    INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = @SaleType
      AND ISNULL(t.IsReturn, 0) = 0
      AND ti.PartId IS NOT NULL
    GROUP BY ti.PartId
),
StockByPart AS
(
    SELECT
        PartId,
        SUM(ISNULL(Quantity, 0)) AS OnHand,
        SUM(ISNULL(Quantity, 0) - ISNULL(ReservedQuantity, 0)) AS AvailableQuantity
    FROM dbo.Stock
    GROUP BY PartId
),
ReceiptByPart AS
(
    SELECT
        PartId,
        MAX(CreatedAt) AS LastReceivedAt
    FROM dbo.StockMovements
    WHERE Quantity > 0
    GROUP BY PartId
)
SELECT TOP (@Take)
    p.Id AS PartId,
    p.InternalCode,
    p.Name AS PartName,
    p.OEMNumber AS OemNumber,
    ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
    p.SalePrice,
    COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0) AS UnitCost,
    ISNULL(st.OnHand, 0) AS OnHand,
    ISNULL(st.AvailableQuantity, 0) AS AvailableQuantity,
    ISNULL(st.OnHand, 0) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0) AS StockValue,
    ISNULL(s.SoldQuantityLast90, 0) AS SoldQuantityLast90,
    ISNULL(s.SoldQuantityAllTime, 0) AS SoldQuantityAllTime,
    s.LastSoldAt,
    r.LastReceivedAt,
    activity.DormantSince,
    DATEDIFF(DAY, activity.DormantSince, @Today) AS DormantDays
FROM dbo.Parts p
LEFT JOIN StockByPart st ON st.PartId = p.Id
LEFT JOIN SalesByPart s ON s.PartId = p.Id
LEFT JOIN ReceiptByPart r ON r.PartId = p.Id
CROSS APPLY
(
    SELECT MAX(value) AS DormantSince
    FROM
    (
        VALUES
            (CAST(s.LastSoldAt AS DATETIME2)),
            (CAST(r.LastReceivedAt AS DATETIME2)),
            (CAST(p.CreatedAt AS DATETIME2))
    ) AS dates(value)
) activity
WHERE p.IsActive = 1
  AND ISNULL(st.OnHand, 0) > 0
  AND DATEDIFF(DAY, activity.DormantSince, @Today) >= @MinDormantDays
ORDER BY
    DATEDIFF(DAY, activity.DormantSince, @Today) DESC,
    ISNULL(st.OnHand, 0) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0) DESC,
    p.Name;
""",
            new
            {
                Today = today,
                RecentCutoff = recentCutoff,
                SaleType = TransactionTypeKeys.Sale,
                MinDormantDays = minDormantDays,
                Take = take
            },
            session.Transaction)
            .ToList();

        session.Commit();

        var items = rows.Select(row =>
        {
            var actions = BuildDeadStockActions(row);
            return new DeadStockItemDto
            {
                PartId = row.PartId,
                InternalCode = row.InternalCode,
                PartName = row.PartName,
                OemNumber = row.OemNumber,
                Currency = NormalizeCurrency(row.Currency),
                SalePrice = row.SalePrice,
                UnitCost = row.UnitCost,
                OnHand = row.OnHand,
                AvailableQuantity = row.AvailableQuantity,
                StockValue = row.StockValue,
                SoldQuantityLast90 = row.SoldQuantityLast90,
                SoldQuantityAllTime = row.SoldQuantityAllTime,
                LastSoldAt = row.LastSoldAt,
                LastReceivedAt = row.LastReceivedAt,
                DormantSince = row.DormantSince,
                DormantDays = row.DormantDays,
                PrimaryAction = actions.FirstOrDefault()?.Label ?? "Review",
                SuggestedActions = actions
            };
        }).ToList();

        var currencies = items
            .Select(item => NormalizeCurrency(item.Currency))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DeadStockReportDto
        {
            GeneratedAt = DateTime.UtcNow,
            MinDormantDays = minDormantDays,
            TotalCandidates = items.Count,
            TotalStockValue = currencies.Count == 1 ? items.Sum(item => item.StockValue) : 0m,
            DominantCurrency = currencies.Count == 1 ? currencies[0] : "MIXED",
            Items = items
        };
    }

    public int Create(CreatePartRequest request, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        ValidateUsedCar(session, request.UsedCarId);

        var activePartsCount = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.Parts WHERE IsActive = 1 AND TenantId = @TenantId",
            new { TenantId = _tenantContext.TenantId },
            session.Transaction);
        _subscriptionLimitService.EnsureWithinLimit(_tenantContext.TenantId, LimitCode.ActivePartsCount, activePartsCount, "Active Parts");

        var repository = new PartsRepository(session);
        var part = new Part
        {
            InternalCode = request.InternalCode,
            Barcode = NormalizeOptional(request.Barcode),
            Name = request.Name,
            OEMNumber = request.OEMNumber,
            Condition = request.Condition,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            AveragePrice = request.AveragePrice,
            EstimatedMarketPrice = request.EstimatedMarketPrice,
            CostAllocationPercent = request.CostAllocationPercent,
            AllocatedCost = request.AllocatedCost,
            MinimumSellPrice = request.MinimumSellPrice,
            FastSalePrice = request.FastSalePrice,
            WholesalePrice = request.WholesalePrice,
            RecommendedPrice = request.RecommendedPrice,
            PricingStatus = NormalizePricingStatus(request.PricingStatus),
            PricingCalculatedAt = request.PricingCalculatedAt,
            Currency = request.Currency,
            MinStock = request.MinStock,
            Notes = request.Notes,
            UsedCarId = request.UsedCarId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(part);
        if (request.UsedCarId is int usedCarId)
        {
            UsedCarPartPricingAllocator.RepriceUsedCarParts(session, usedCarId, userId);
            EnsureInitialUsedCarPartStock(session, id, usedCarId, userId);
        }

        session.Commit();
        return id;
    }

    public void Update(int id, CreatePartRequest request, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        ValidateUsedCar(session, request.UsedCarId);
        var previousUsedCarId = LoadPartUsedCarId(session, id);

        var repository = new PartsRepository(session);
        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Part not found.");
        }

        if (request.UsedCarId is int usedCarId)
        {
            if (previousUsedCarId is int oldUsedCarId && oldUsedCarId != usedCarId)
            {
                UsedCarPartPricingAllocator.RepriceUsedCarParts(session, oldUsedCarId, userId);
            }

            UsedCarPartPricingAllocator.RepriceUsedCarParts(session, usedCarId, userId);
            EnsureInitialUsedCarPartStock(session, id, usedCarId, userId);
        }
        else if (previousUsedCarId is int oldUsedCarId)
        {
            UsedCarPartPricingAllocator.ClearPartAllocation(session, id, userId);
            UsedCarPartPricingAllocator.RepriceUsedCarParts(session, oldUsedCarId, userId);
        }

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var repository = new PartsRepository(session);
        if (!repository.Delete(id))
        {
            throw new NotFoundException("Part not found.");
        }

        session.Commit();
    }

    public PartListingPackageDto BuildListingPackage(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var row = session.Connection.QuerySingleOrDefault<PartListingQueryRow>(
            """
            SELECT
                p.Id,
                p.Name,
                p.OEMNumber,
                p.Condition,
                p.SalePrice,
                ISNULL(NULLIF(p.Currency, N''), N'USD') AS Currency,
                p.UsedCarId,
                uc.Car AS UsedCarName
            FROM dbo.Parts p
            LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
            WHERE p.Id = @PartId AND p.IsActive = 1;
            """,
            new { PartId = partId },
            session.Transaction);

        if (row is null)
        {
            throw new NotFoundException($"Part {partId} not found.");
        }

        var photoCount = row.UsedCarId is int usedCarId
            ? session.Connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM dbo.usedcar_images WHERE UsedCarId = @UsedCarId",
                new { UsedCarId = usedCarId },
                session.Transaction)
            : 0;

        session.Commit();

        var priceText = $"{row.Currency} {row.SalePrice:N0}";
        var conditionText = row.Condition.ToString();
        var title = $"{row.Name} ({row.OEMNumber}) - {conditionText} - {priceText}";
        var sourceLine = string.IsNullOrWhiteSpace(row.UsedCarName)
            ? string.Empty
            : $"Removed from a {row.UsedCarName}.\n";
        var hashtag = row.Name.Replace(" ", string.Empty);
        var description =
            $"{row.Name}\n" +
            $"OEM Number: {row.OEMNumber}\n" +
            $"Condition: {conditionText}\n" +
            $"Price: {priceText}\n" +
            sourceLine +
            $"\nGenuine auto part in {conditionText.ToLowerInvariant()} condition, ready to ship or pick up. " +
            "Contact us for more details, fitment confirmation, or a closer look.\n\n" +
            $"#AutoParts #{hashtag} #SpareParts #ForSale";

        return new PartListingPackageDto
        {
            PartId = row.Id,
            Title = title,
            Description = description,
            PriceText = priceText,
            PhotoCount = photoCount,
            MarketplaceLinks =
            [
                new MarketplaceLinkDto { Name = "Facebook Marketplace", Url = "https://www.facebook.com/marketplace/create/item" },
                new MarketplaceLinkDto { Name = "OLX", Url = "https://www.olx.com/" },
                new MarketplaceLinkDto { Name = "Dubizzle", Url = "https://www.dubizzle.com/" }
            ]
        };
    }

    private sealed class PartListingQueryRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OEMNumber { get; set; } = string.Empty;
        public PartCondition Condition { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int? UsedCarId { get; set; }
        public string? UsedCarName { get; set; }
    }

    public void UpdateUsedCar(int id, int? usedCarId, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        ValidateUsedCar(session, usedCarId);
        var previousUsedCarId = LoadPartUsedCarId(session, id);

        var repository = new PartsRepository(session);
        if (!repository.UpdateUsedCarId(id, usedCarId, userId))
        {
            throw new NotFoundException("Part not found.");
        }

        if (usedCarId is int validUsedCarId)
        {
            if (previousUsedCarId is int oldUsedCarId && oldUsedCarId != validUsedCarId)
            {
                UsedCarPartPricingAllocator.RepriceUsedCarParts(session, oldUsedCarId, userId);
            }

            UsedCarPartPricingAllocator.RepriceUsedCarParts(session, validUsedCarId, userId);
            EnsureInitialUsedCarPartStock(session, id, validUsedCarId, userId);
        }
        else if (previousUsedCarId is int oldUsedCarId)
        {
            UsedCarPartPricingAllocator.ClearPartAllocation(session, id, userId);
            UsedCarPartPricingAllocator.RepriceUsedCarParts(session, oldUsedCarId, userId);
        }

        session.Commit();
    }

    public void TransferStock(int id, TransferPartRequest request, int userId)
    {
        if (request.Quantity <= 0)
        {
            throw new ValidationException("Transfer quantity must be greater than zero.");
        }

        if (request.FromWarehouseId == request.ToWarehouseId)
        {
            throw new ValidationException("Choose two different warehouses for a stock transfer.");
        }

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        EnsurePartExists(session, id);
        EnsureWarehouseExists(session, request.FromWarehouseId, "Source warehouse");
        EnsureWarehouseExists(session, request.ToWarehouseId, "Destination warehouse");

        var unitCost = session.Connection.ExecuteScalar<decimal?>(
            "SELECT CostPrice FROM dbo.Parts WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, session.TenantId },
            session.Transaction) ?? 0m;

        var inventoryRepository = new InventoryRepository(session);
        _inventoryService.AdjustStock(
            inventoryRepository,
            id,
            request.FromWarehouseId,
            -request.Quantity,
            StockMovementType.TransferOut,
            DomainReferenceType.Transfer,
            null,
            unitCost,
            userId);
        _inventoryService.AdjustStock(
            inventoryRepository,
            id,
            request.ToWarehouseId,
            request.Quantity,
            StockMovementType.TransferIn,
            DomainReferenceType.Transfer,
            null,
            unitCost,
            userId);

        session.Commit();
    }

    public Task<GeneratePartNotesResponse> GenerateNotesAsync(
        GeneratePartNotesRequest request,
        CancellationToken cancellationToken = default)
        => _partNotesAiService.GenerateNotesAsync(request, cancellationToken);

    private static void ValidateUsedCar(DbSession session, int? usedCarId)
    {
        if (usedCarId is not int validUsedCarId || validUsedCarId <= 0)
        {
            return;
        }

        var exists = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = validUsedCarId, session.TenantId },
            session.Transaction);
        if (exists == 0)
        {
            throw new ValidationException("Selected used car was not found.");
        }
    }

    private static void EnsurePartExists(DbSession session, int partId)
    {
        var exists = session.Connection.ExecuteScalar<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Parts WHERE Id = @Id AND IsActive = 1 AND (@TenantId = 0 OR TenantId = @TenantId)) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;",
            new { Id = partId, session.TenantId },
            session.Transaction);
        if (!exists)
        {
            throw new NotFoundException("Part not found.");
        }
    }

    private static int? LoadPartUsedCarId(DbSession session, int partId)
    {
        return session.Connection.ExecuteScalar<int?>(
            "SELECT UsedCarId FROM dbo.Parts WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { PartId = partId, session.TenantId },
            session.Transaction);
    }

    private static void EnsureWarehouseExists(DbSession session, int warehouseId, string label)
    {
        var exists = session.Connection.ExecuteScalar<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Warehouses WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;",
            new { Id = warehouseId, session.TenantId },
            session.Transaction);
        if (!exists)
        {
            throw new ValidationException($"{label} was not found.");
        }
    }

    private static void EnsureInitialUsedCarPartStock(DbSession session, int partId, int usedCarId, int userId)
    {
        var currentStock = session.Connection.ExecuteScalar<int>(
            "SELECT ISNULL(SUM(Quantity), 0) FROM dbo.Stock WHERE PartId = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { PartId = partId, session.TenantId },
            session.Transaction);

        if (currentStock >= UsedCarPartInitialStockQuantity || HasSaleHistory(session, partId))
        {
            return;
        }

        var warehouseId = ResolveDefaultWarehouseId(session);
        var quantityToAdd = UsedCarPartInitialStockQuantity - currentStock;
        var locationId = ResolveDefaultStockLocationId(session, warehouseId, usedCarId);
        var unitCost = session.Connection.ExecuteScalar<decimal?>(
            "SELECT CostPrice FROM dbo.Parts WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { PartId = partId, session.TenantId },
            session.Transaction) ?? 0m;

        var inventoryRepository = new InventoryRepository(session);
        var existingStock = inventoryRepository.GetStock(partId, warehouseId);
        if (existingStock == null)
        {
            inventoryRepository.InsertStock(new Stock
            {
                PartId = partId,
                WarehouseId = warehouseId,
                LocationId = locationId,
                Quantity = quantityToAdd,
                ReservedQuantity = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });
        }
        else
        {
            inventoryRepository.UpdateStockQuantity(existingStock.Id, quantityToAdd, userId);
        }

        inventoryRepository.InsertStockMovement(new StockMovement
        {
            PartId = partId,
            WarehouseId = warehouseId,
            Quantity = quantityToAdd,
            MovementType = StockMovementType.Adjust,
            ReferenceType = UsedCarStockReferenceType,
            ReferenceId = usedCarId,
            UnitCost = unitCost,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private static int ResolveDefaultWarehouseId(DbSession session)
    {
        var warehouseId = session.Connection.ExecuteScalar<int?>(
            @"SELECT TOP (1) Id
              FROM dbo.Warehouses
              WHERE (@TenantId = 0 OR TenantId = @TenantId)
              ORDER BY CASE WHEN IsMain = 1 THEN 0 ELSE 1 END, Id;",
            new { session.TenantId },
            session.Transaction);

        if (warehouseId is > 0)
        {
            return warehouseId.Value;
        }

        throw new ValidationException("Create a warehouse before assigning used-car parts to stock.");
    }

    private static int? ResolveDefaultStockLocationId(DbSession session, int warehouseId, int usedCarId)
    {
        return session.Connection.ExecuteScalar<int?>(
            """
IF OBJECT_ID('dbo.Locations', 'U') IS NULL
BEGIN
    SELECT CAST(NULL AS INT);
END
ELSE
BEGIN
    SELECT TOP (1) stockLocation.Id
    FROM dbo.Locations stockLocation
    OUTER APPLY
    (
        SELECT TOP (1) Location
        FROM dbo.UsedCars
        WHERE Id = @UsedCarId
          AND (@TenantId = 0 OR TenantId = @TenantId)
    ) usedCar
    WHERE stockLocation.WarehouseId = @WarehouseId
      AND (@TenantId = 0 OR stockLocation.TenantId = @TenantId)
    ORDER BY
        CASE
            WHEN NULLIF(LTRIM(RTRIM(usedCar.Location)), N'') IS NOT NULL
             AND (
                 UPPER(LTRIM(RTRIM(stockLocation.Code))) = UPPER(LTRIM(RTRIM(usedCar.Location)))
                 OR UPPER(LTRIM(RTRIM(ISNULL(stockLocation.Description, N'')))) = UPPER(LTRIM(RTRIM(usedCar.Location)))
             )
            THEN 0
            ELSE 1
        END,
        stockLocation.Id;
END;
""",
            new { WarehouseId = warehouseId, UsedCarId = usedCarId, session.TenantId },
            session.Transaction);
    }

    private static bool HasSaleHistory(DbSession session, int partId)
    {
        return session.Connection.ExecuteScalar<bool>(
            @"SELECT CASE WHEN EXISTS
              (
                  SELECT 1
                  FROM dbo.StockMovements
                  WHERE PartId = @PartId
                    AND Quantity < 0
                    AND (@TenantId = 0 OR TenantId = @TenantId)
                    AND (
                        UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(50), MovementType)))) = N'SALE'
                        OR TRY_CONVERT(INT, CONVERT(NVARCHAR(50), MovementType)) = 2
                    )
              )
              THEN CAST(1 AS BIT)
              ELSE CAST(0 AS BIT)
              END;",
            new { PartId = partId, session.TenantId },
            session.Transaction);
    }

    private static List<DeadStockActionDto> BuildDeadStockActions(DeadStockQueryRow row)
    {
        var discountPercent = row.DormantDays >= 365 ? 25 : 15;
        var discountedPrice = row.SalePrice * (100 - discountPercent) / 100;
        var costText = FormatMoney(row.UnitCost, row.Currency);
        var actions = new Dictionary<string, DeadStockActionDto>(StringComparer.OrdinalIgnoreCase)
        {
            ["discount"] = new()
            {
                Key = "discount",
                Label = $"Discount {discountPercent}%",
                Tone = discountedPrice >= row.UnitCost ? "Good" : "Warning",
                Detail = discountedPrice >= row.UnitCost
                    ? $"Run a {discountPercent}% clearance price at {FormatMoney(discountedPrice, row.Currency)} for 14 days; estimated unit cost is {costText}."
                    : $"Use a guarded discount only if approved; a {discountPercent}% cut lands near estimated unit cost {costText}."
            },
            ["bundle"] = new()
            {
                Key = "bundle",
                Label = "Bundle",
                Tone = row.OnHand >= 3 ? "Good" : "Neutral",
                Detail = row.OnHand >= 3
                    ? $"Bundle {row.OnHand:N0} unit(s) with fast-moving service parts or donor-car kits to raise basket value."
                    : "Pair this with a compatible fast mover so the slow item leaves stock without becoming the headline discount."
            },
            ["whatsapp"] = new()
            {
                Key = "whatsapp",
                Label = "WhatsApp campaign",
                Tone = "Good",
                Detail = $"Send a short campaign featuring {PartLabel(row)}, OEM {FormatOem(row.OemNumber)}, and the current price {FormatMoney(row.SalePrice, row.Currency)}."
            },
            ["supplier_return"] = new()
            {
                Key = "supplier_return",
                Label = "Supplier return",
                Tone = row.DormantDays >= 365 || row.SoldQuantityAllTime <= 0 ? "Warning" : "Neutral",
                Detail = $"Ask for credit, swap, or consignment return; current on-hand value is {FormatMoney(row.StockValue, row.Currency)}."
            },
            ["marketplace"] = new()
            {
                Key = "marketplace",
                Label = "Marketplace listing",
                Tone = row.SoldQuantityAllTime <= 0 ? "Warning" : "Good",
                Detail = $"List with code {DisplayCode(row.InternalCode, row.PartId)}, OEM {FormatOem(row.OemNumber)}, dormant {row.DormantDays:N0} days, and clear pickup/shipping terms."
            }
        };

        var preferredKey =
            row.SoldQuantityAllTime <= 0 ? "marketplace" :
            row.DormantDays >= 365 && row.StockValue >= 300m ? "supplier_return" :
            row.OnHand >= 3 ? "bundle" :
            row.SalePrice > row.UnitCost * 1.15m ? "discount" :
            "whatsapp";

        return new[] { preferredKey, "discount", "bundle", "whatsapp", "supplier_return", "marketplace" }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(key => actions[key])
            .ToList();
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizePricingStatus(string? value)
        => string.IsNullOrWhiteSpace(value) ? PartPricingStatus.Manual : value.Trim();

    private static string NormalizeCurrency(string? currencyCode)
        => string.IsNullOrWhiteSpace(currencyCode) ? PartDefaults.Currency : currencyCode.Trim().ToUpperInvariant();

    private static string FormatMoney(decimal amount, string? currencyCode)
        => $"{amount:N2} {NormalizeCurrency(currencyCode)}";

    private static string DisplayCode(string? internalCode, int partId)
        => string.IsNullOrWhiteSpace(internalCode) ? $"PART-{partId}" : internalCode.Trim();

    private static string FormatOem(string? oemNumber)
        => string.IsNullOrWhiteSpace(oemNumber) ? "not set" : oemNumber.Trim();

    private static string PartLabel(DeadStockQueryRow row)
        => string.IsNullOrWhiteSpace(row.InternalCode)
            ? row.PartName
            : $"{row.InternalCode} - {row.PartName}";

    private sealed class DeadStockQueryRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string Currency { get; set; } = PartDefaults.Currency;
        public decimal SalePrice { get; set; }
        public decimal UnitCost { get; set; }
        public decimal OnHand { get; set; }
        public decimal AvailableQuantity { get; set; }
        public decimal StockValue { get; set; }
        public decimal SoldQuantityLast90 { get; set; }
        public decimal SoldQuantityAllTime { get; set; }
        public DateTime? LastSoldAt { get; set; }
        public DateTime? LastReceivedAt { get; set; }
        public DateTime DormantSince { get; set; }
        public int DormantDays { get; set; }
    }
}
