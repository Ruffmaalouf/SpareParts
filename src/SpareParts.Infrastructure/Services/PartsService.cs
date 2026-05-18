using Dapper;
using SpareParts.Domain.Common;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class PartsService
{
    private const int UsedCarPartInitialStockQuantity = 1;
    private const string UsedCarStockReferenceType = "UsedCar";

    private readonly ISqlConnectionFactory _factory;
    private readonly PartNotesAiService _partNotesAiService;

    public PartsService(ISqlConnectionFactory factory, PartNotesAiService partNotesAiService)
    {
        _factory = factory;
        _partNotesAiService = partNotesAiService;
    }

    public (IEnumerable<PartDto> Items, int TotalCount) GetAll(int page, int pageSize, int? usedCarId = null)
    {
        using var session = new DbSession(_factory);
        var repository = new PartsRepository(session);
        var projected = repository.GetAllActive(usedCarId).Select(p => new PartDto
        {
            Id = p.Id,
            InternalCode = p.InternalCode,
            Barcode = p.Barcode,
            Name = p.Name,
            OEMNumber = p.OEMNumber,
            Condition = p.Condition,
            CategoryId = p.CategoryId,
            BrandId = p.BrandId,
            CostPrice = p.CostPrice,
            SalePrice = p.SalePrice,
            AveragePrice = p.AveragePrice,
            Currency = p.Currency,
            MinStock = p.MinStock,
            Notes = p.Notes,
            UsedCarId = p.UsedCarId,
            IsActive = p.IsActive
        }).ToList();

        HydrateStockQuantities(session, projected);

        var paged = projected.Skip((page - 1) * pageSize).Take(pageSize);
        return (paged, projected.Count);
    }

    public IReadOnlyList<PartStockDto> GetStockByWarehouse(int partId)
    {
        using var session = new DbSession(_factory);
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

        using var session = new DbSession(_factory);
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
    COALESCE(NULLIF(p.AveragePrice, 0), NULLIF(p.CostPrice, 0), 0) AS UnitCost,
    ISNULL(st.OnHand, 0) AS OnHand,
    ISNULL(st.AvailableQuantity, 0) AS AvailableQuantity,
    ISNULL(st.OnHand, 0) * COALESCE(NULLIF(p.AveragePrice, 0), NULLIF(p.CostPrice, 0), 0) AS StockValue,
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
    ISNULL(st.OnHand, 0) * COALESCE(NULLIF(p.AveragePrice, 0), NULLIF(p.CostPrice, 0), 0) DESC,
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
        using var session = new DbSession(_factory);
        ValidateUsedCar(session, request.UsedCarId);

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
            EnsureInitialUsedCarPartStock(session, id, usedCarId, userId);
        }

        session.Commit();
        return id;
    }

    public void Update(int id, CreatePartRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        ValidateUsedCar(session, request.UsedCarId);

        var repository = new PartsRepository(session);
        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Part not found.");
        }

        if (request.UsedCarId is int usedCarId)
        {
            EnsureInitialUsedCarPartStock(session, id, usedCarId, userId);
        }

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory);
        var repository = new PartsRepository(session);
        if (!repository.Delete(id))
        {
            throw new NotFoundException("Part not found.");
        }

        session.Commit();
    }

    public void UpdateUsedCar(int id, int? usedCarId, int userId)
    {
        using var session = new DbSession(_factory);
        ValidateUsedCar(session, usedCarId);

        var repository = new PartsRepository(session);
        if (!repository.UpdateUsedCarId(id, usedCarId, userId))
        {
            throw new NotFoundException("Part not found.");
        }

        if (usedCarId is int validUsedCarId)
        {
            EnsureInitialUsedCarPartStock(session, id, validUsedCarId, userId);
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

        using var session = new DbSession(_factory);
        EnsurePartExists(session, id);
        EnsureWarehouseExists(session, request.FromWarehouseId, "Source warehouse");
        EnsureWarehouseExists(session, request.ToWarehouseId, "Destination warehouse");

        var unitCost = session.Connection.ExecuteScalar<decimal?>(
            "SELECT CostPrice FROM dbo.Parts WHERE Id = @Id;",
            new { Id = id },
            session.Transaction) ?? 0m;

        var inventoryRepository = new InventoryRepository(session);
        var inventoryService = new InventoryService();
        inventoryService.AdjustStock(
            inventoryRepository,
            id,
            request.FromWarehouseId,
            -request.Quantity,
            StockMovementType.TransferOut,
            DomainReferenceType.Transfer,
            null,
            unitCost,
            userId);
        inventoryService.AdjustStock(
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
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @Id;",
            new { Id = validUsedCarId },
            session.Transaction);
        if (exists == 0)
        {
            throw new ValidationException("Selected used car was not found.");
        }
    }

    private static void EnsurePartExists(DbSession session, int partId)
    {
        var exists = session.Connection.ExecuteScalar<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Parts WHERE Id = @Id AND IsActive = 1) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;",
            new { Id = partId },
            session.Transaction);
        if (!exists)
        {
            throw new NotFoundException("Part not found.");
        }
    }

    private static void EnsureWarehouseExists(DbSession session, int warehouseId, string label)
    {
        var exists = session.Connection.ExecuteScalar<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Warehouses WHERE Id = @Id) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;",
            new { Id = warehouseId },
            session.Transaction);
        if (!exists)
        {
            throw new ValidationException($"{label} was not found.");
        }
    }

    private static void EnsureInitialUsedCarPartStock(DbSession session, int partId, int usedCarId, int userId)
    {
        var currentStock = session.Connection.ExecuteScalar<int>(
            "SELECT ISNULL(SUM(Quantity), 0) FROM dbo.Stock WHERE PartId = @PartId;",
            new { PartId = partId },
            session.Transaction);

        if (currentStock >= UsedCarPartInitialStockQuantity || HasSaleHistory(session, partId))
        {
            return;
        }

        var warehouseId = ResolveDefaultWarehouseId(session);
        var quantityToAdd = UsedCarPartInitialStockQuantity - currentStock;
        var locationId = ResolveDefaultStockLocationId(session, warehouseId, usedCarId);
        var unitCost = session.Connection.ExecuteScalar<decimal?>(
            "SELECT CostPrice FROM dbo.Parts WHERE Id = @PartId;",
            new { PartId = partId },
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
              ORDER BY CASE WHEN IsMain = 1 THEN 0 ELSE 1 END, Id;",
            transaction: session.Transaction);

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
    ) usedCar
    WHERE stockLocation.WarehouseId = @WarehouseId
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
            new { WarehouseId = warehouseId, UsedCarId = usedCarId },
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
                    AND (MovementType IN (N'Sale', N'2') OR TRY_CONVERT(INT, MovementType) = 2)
              )
              THEN CAST(1 AS BIT)
              ELSE CAST(0 AS BIT)
              END;",
            new { PartId = partId },
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

    private static void HydrateStockQuantities(DbSession session, List<PartDto> parts)
    {
        if (parts.Count == 0)
        {
            return;
        }

        var stockByPart = session.Connection.Query<PartStockSummary>(
            """
SELECT
    PartId,
    Quantity = ISNULL(SUM(Quantity), 0),
    ReservedQuantity = ISNULL(SUM(ReservedQuantity), 0)
FROM dbo.Stock
WHERE PartId IN @PartIds
GROUP BY PartId;
""",
            new { PartIds = parts.Select(part => part.Id).ToArray() },
            session.Transaction)
            .ToDictionary(stock => stock.PartId);

        foreach (var part in parts)
        {
            if (!stockByPart.TryGetValue(part.Id, out var stock))
            {
                continue;
            }

            part.StockQuantity = stock.Quantity;
            part.ReservedQuantity = stock.ReservedQuantity;
            part.AvailableQuantity = stock.Quantity - stock.ReservedQuantity;
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeCurrency(string? currencyCode)
        => string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();

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

    private sealed class PartStockSummary
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
    }

    private sealed class DeadStockQueryRow
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string Currency { get; set; } = "USD";
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
