using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class UsedCarTwinService
{
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Inspection", "MileageUpdate", "ConditionChange", "LocationMove", "Note"
    };

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly UsedCarsService _usedCarsService;

    public UsedCarTwinService(
        ISqlConnectionFactory factory,
        ITenantContext tenantContext,
        UsedCarsService usedCarsService)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _usedCarsService = usedCarsService;
    }

    public UsedCarTwinDto GetTwin(int usedCarId)
    {
        var car = _usedCarsService.GetAll().FirstOrDefault(c => c.Id == usedCarId)
            ?? throw new NotFoundException($"Used car {usedCarId} not found.");

        using var conn = _factory.CreateConnection();
        var lifecycle = conn.QuerySingleOrDefault<UsedCarLifecycleDates>(
            @"SELECT CreatedAt, ReceivedAt
              FROM dbo.UsedCars
              WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = usedCarId, TenantId = _tenantContext.TenantId })
            ?? new UsedCarLifecycleDates();

        var imageCount = conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM dbo.usedcar_images WHERE UsedCarId = @UsedCarId;",
            new { UsedCarId = usedCarId });

        var parts = LoadParts(conn, usedCarId);
        var wholesaleSale = _usedCarsService.GetWholesaleSales().FirstOrDefault(w => w.UsedCarId == usedCarId);
        var stateEvents = LoadStateEvents(conn, usedCarId);
        var timeline = BuildTimeline(car, lifecycle, parts, wholesaleSale, stateEvents);

        return new UsedCarTwinDto
        {
            Car = car,
            ImageCount = imageCount,
            Parts = parts,
            WholesaleSale = wholesaleSale,
            StateEvents = stateEvents,
            Timeline = timeline
        };
    }

    public IEnumerable<UsedCarStateEventDto> GetStateEvents(int usedCarId)
    {
        using var conn = _factory.CreateConnection();
        EnsureUsedCarExists(conn, usedCarId);
        return LoadStateEvents(conn, usedCarId);
    }

    public UsedCarStateEventDto AddStateEvent(int usedCarId, CreateUsedCarStateEventRequest request, int userId)
    {
        var eventType = request.EventType?.Trim() ?? string.Empty;
        if (!AllowedEventTypes.Contains(eventType))
        {
            throw new ValidationException($"Event type must be one of: {string.Join(", ", AllowedEventTypes)}.");
        }

        if (request.Mileage is < 0)
        {
            throw new ValidationException("Mileage cannot be negative.");
        }

        using var conn = _factory.CreateConnection();
        EnsureUsedCarExists(conn, usedCarId);

        var recordedAt = request.RecordedAt ?? DateTime.UtcNow;
        var id = conn.ExecuteScalar<int>(
            @"INSERT INTO dbo.UsedCarStateEvents
                (UsedCarId, EventType, Mileage, Condition, Location, Note, RecordedAt, CreatedByUserId)
              VALUES
                (@UsedCarId, @EventType, @Mileage, @Condition, @Location, @Note, @RecordedAt, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                UsedCarId = usedCarId,
                EventType = eventType,
                request.Mileage,
                Condition = NormalizeOptional(request.Condition),
                Location = NormalizeOptional(request.Location),
                Note = NormalizeOptional(request.Note),
                RecordedAt = recordedAt,
                UserId = userId
            });

        return LoadStateEvents(conn, usedCarId).First(e => e.Id == id);
    }

    private static List<UsedCarTwinPartDto> LoadParts(System.Data.IDbConnection conn, int usedCarId)
        => conn.Query<UsedCarTwinPartRow>(
            @"SELECT p.Id AS PartId,
                     p.Name,
                     p.InternalCode,
                     p.OEMNumber AS OemNumber,
                     p.Condition,
                     p.CreatedAt AS ListedAt,
                     RemainingQuantity = ISNULL((SELECT SUM(CAST(st.Quantity AS DECIMAL(19, 4))) FROM dbo.Stock st WHERE st.PartId = p.Id), 0),
                     sale.SoldAt,
                     sale.SoldAmountBase
              FROM dbo.Parts p
              OUTER APPLY
              (
                  SELECT TOP (1)
                         SoldAt = t.TransactionDate,
                         SoldAmountBase = CASE WHEN t.IsReturn = 1 THEN -ISNULL(ti.BaseAmount, ti.LineTotal) ELSE ISNULL(ti.BaseAmount, ti.LineTotal) END
                  FROM dbo.Transactions t
                  INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = N'sale'
                  INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id AND ti.PartId = p.Id
                  ORDER BY t.TransactionDate DESC, t.Id DESC
              ) sale
              WHERE p.UsedCarId = @UsedCarId
              ORDER BY p.CreatedAt DESC;",
            new { UsedCarId = usedCarId })
            .Select(MapPart)
            .ToList();

    private static UsedCarTwinPartDto MapPart(UsedCarTwinPartRow row)
        => new()
        {
            PartId = row.PartId,
            Name = row.Name,
            InternalCode = row.InternalCode,
            OemNumber = row.OemNumber,
            Condition = ((SpareParts.Domain.Inventory.PartCondition)row.Condition).ToString(),
            ListedAt = row.ListedAt,
            RemainingQuantity = row.RemainingQuantity,
            IsSold = row.SoldAt.HasValue,
            SoldAt = row.SoldAt,
            SoldAmountBase = row.SoldAmountBase
        };

    private static List<UsedCarStateEventDto> LoadStateEvents(System.Data.IDbConnection conn, int usedCarId)
        => conn.Query<UsedCarStateEventDto>(
            @"SELECT e.Id,
                     e.UsedCarId,
                     e.EventType,
                     e.Mileage,
                     e.Condition,
                     e.Location,
                     e.Note,
                     e.RecordedAt,
                     e.CreatedAt,
                     e.CreatedByUserId,
                     CreatedByUserName = COALESCE(NULLIF(LTRIM(RTRIM(u.FullName)), N''), u.Username)
              FROM dbo.UsedCarStateEvents e
              LEFT JOIN dbo.Users u ON u.Id = e.CreatedByUserId
              WHERE e.UsedCarId = @UsedCarId
              ORDER BY e.RecordedAt DESC, e.Id DESC;",
            new { UsedCarId = usedCarId })
            .ToList();

    private static List<UsedCarTwinEventDto> BuildTimeline(
        UsedCarDto car,
        UsedCarLifecycleDates lifecycle,
        IReadOnlyCollection<UsedCarTwinPartDto> parts,
        UsedCarWholesaleSaleDto? wholesaleSale,
        IReadOnlyCollection<UsedCarStateEventDto> stateEvents)
    {
        var timeline = new List<UsedCarTwinEventDto>
        {
            new()
            {
                EventType = "Purchased",
                OccurredAt = lifecycle.CreatedAt,
                Title = "Added to inventory",
                Description = $"Purchased for {car.PriceCurrency} {car.Price:N0}",
                AmountBase = car.PriceBase
            }
        };

        if (car.IsReceived && lifecycle.ReceivedAt.HasValue)
        {
            timeline.Add(new UsedCarTwinEventDto
            {
                EventType = "Received",
                OccurredAt = lifecycle.ReceivedAt.Value,
                Title = "Vehicle received",
                Description = string.IsNullOrWhiteSpace(car.Location) ? null : $"Received at {car.Location}"
            });
        }

        foreach (var part in parts)
        {
            timeline.Add(new UsedCarTwinEventDto
            {
                EventType = "PartListed",
                OccurredAt = part.ListedAt,
                Title = "Part listed for resale",
                Description = $"{part.Name} ({part.InternalCode})"
            });

            if (part.SoldAt.HasValue)
            {
                timeline.Add(new UsedCarTwinEventDto
                {
                    EventType = "PartSold",
                    OccurredAt = part.SoldAt.Value,
                    Title = "Part sold",
                    Description = $"{part.Name} ({part.InternalCode})",
                    AmountBase = part.SoldAmountBase
                });
            }
        }

        if (wholesaleSale != null)
        {
            timeline.Add(new UsedCarTwinEventDto
            {
                EventType = "WholesaleSale",
                OccurredAt = wholesaleSale.SaleDate,
                Title = "Sold wholesale",
                Description = $"Sold to {wholesaleSale.BuyerName}",
                AmountBase = wholesaleSale.SalePriceBase
            });
        }

        foreach (var stateEvent in stateEvents)
        {
            timeline.Add(new UsedCarTwinEventDto
            {
                EventType = stateEvent.EventType,
                OccurredAt = stateEvent.RecordedAt,
                Title = FormatStateEventTitle(stateEvent),
                Description = FormatStateEventDescription(stateEvent),
                IsManual = true
            });
        }

        return timeline.OrderBy(e => e.OccurredAt).ToList();
    }

    private static string FormatStateEventTitle(UsedCarStateEventDto stateEvent)
        => stateEvent.EventType switch
        {
            "MileageUpdate" => "Mileage updated",
            "ConditionChange" => "Condition changed",
            "LocationMove" => "Moved location",
            "Inspection" => "Inspection logged",
            _ => "Note added"
        };

    private static string? FormatStateEventDescription(UsedCarStateEventDto stateEvent)
    {
        var parts = new List<string>();
        if (stateEvent.Mileage.HasValue) parts.Add($"{stateEvent.Mileage:N0} km");
        if (!string.IsNullOrWhiteSpace(stateEvent.Condition)) parts.Add(stateEvent.Condition);
        if (!string.IsNullOrWhiteSpace(stateEvent.Location)) parts.Add(stateEvent.Location);
        if (!string.IsNullOrWhiteSpace(stateEvent.Note)) parts.Add(stateEvent.Note);
        return parts.Count == 0 ? null : string.Join(" • ", parts);
    }

    private void EnsureUsedCarExists(System.Data.IDbConnection conn, int usedCarId)
    {
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @UsedCarId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { UsedCarId = usedCarId, TenantId = _tenantContext.TenantId });

        if (exists == 0)
        {
            throw new NotFoundException("Used car not found.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
