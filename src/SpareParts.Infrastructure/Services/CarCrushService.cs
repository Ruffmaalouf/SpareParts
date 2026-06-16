using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class CarCrushService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    private static readonly CarCrushPartSuggestion[] StaticSuggestions =
    [
        new() { PartName = "Engine Assembly",                Category = "Drivetrain",    IsHighValue = true,  IsFastMoving = true  },
        new() { PartName = "Transmission / Gearbox",         Category = "Drivetrain",    IsHighValue = true,  IsFastMoving = true  },
        new() { PartName = "Front Bumper",                   Category = "Body",          IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Rear Bumper",                    Category = "Body",          IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Headlight - Left",               Category = "Lighting",      IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Headlight - Right",              Category = "Lighting",      IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Taillight - Left",               Category = "Lighting",      IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Taillight - Right",              Category = "Lighting",      IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Fender - Left",                  Category = "Body",          IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Fender - Right",                 Category = "Body",          IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Hood",                           Category = "Body",          IsHighValue = true,  IsFastMoving = true  },
        new() { PartName = "Door - Front Left",              Category = "Body",          IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Door - Front Right",             Category = "Body",          IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Door - Rear Left",               Category = "Body",          IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Door - Rear Right",              Category = "Body",          IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Trunk Lid / Tailgate",           Category = "Body",          IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Alternator",                     Category = "Electrical",    IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Starter Motor",                  Category = "Electrical",    IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "AC Compressor",                  Category = "HVAC",          IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Radiator",                       Category = "Cooling",       IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "ECU / Engine Control Unit",      Category = "Electronics",   IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Fuel Pump",                      Category = "Fuel System",   IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Mirror - Left",                  Category = "Body",          IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Mirror - Right",                 Category = "Body",          IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Seat - Driver",                  Category = "Interior",      IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Seat - Passenger",               Category = "Interior",      IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Airbag - Driver",                Category = "Safety",        IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Airbag - Passenger",             Category = "Safety",        IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Catalytic Converter",            Category = "Exhaust",       IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Differential",                   Category = "Drivetrain",    IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Transfer Case",                  Category = "Drivetrain",    IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Control Arm - Front Left",       Category = "Suspension",    IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Control Arm - Front Right",      Category = "Suspension",    IsHighValue = false, IsFastMoving = false },
        new() { PartName = "CV Axle - Left",                 Category = "Drivetrain",    IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "CV Axle - Right",                Category = "Drivetrain",    IsHighValue = false, IsFastMoving = true  },
        new() { PartName = "Strut / Shock Absorber - FL",    Category = "Suspension",    IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Strut / Shock Absorber - FR",    Category = "Suspension",    IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Brake Caliper - Front",          Category = "Brakes",        IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Brake Rotor - Front",            Category = "Brakes",        IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Wheel Hub / Bearing",            Category = "Suspension",    IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Tie Rod - Left",                 Category = "Steering",      IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Tie Rod - Right",                Category = "Steering",      IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Steering Rack",                  Category = "Steering",      IsHighValue = true,  IsFastMoving = false },
        new() { PartName = "Power Steering Pump",            Category = "Steering",      IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Water Pump",                     Category = "Cooling",       IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Thermostat",                     Category = "Cooling",       IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Timing Belt / Chain Kit",        Category = "Engine",        IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Oxygen Sensor",                  Category = "Engine",        IsHighValue = false, IsFastMoving = false },
        new() { PartName = "Mass Air Flow Sensor",           Category = "Engine",        IsHighValue = false, IsFastMoving = false },
    ];

    public CarCrushService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public CarCrushResult Generate(CarCrushRequest request)
    {
        var parts = new List<string?> { request.VehicleYear?.ToString(), request.VehicleMake, request.VehicleModel };
        var vehicleDescription = string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p))).Trim();
        if (string.IsNullOrWhiteSpace(vehicleDescription))
            vehicleDescription = "Unknown Vehicle";

        return new CarCrushResult
        {
            VehicleDescription = vehicleDescription,
            Suggestions = StaticSuggestions.ToList(),
            TotalSuggestions = StaticSuggestions.Length,
            Disclaimer = "Suggestions are based on common high-value parts for this vehicle type. Actual available parts may vary."
        };
    }

    public void CreateDraftListings(CarCrushRequest request, List<string> selectedPartNames, int createdByUserId)
    {
        if (selectedPartNames.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var timestamp = now.Ticks;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var warehouseId = session.Connection.ExecuteScalar<int?>(
            """
SELECT TOP 1 Id FROM dbo.Warehouses WHERE IsActive = 1 AND TenantId = @TenantId ORDER BY Id;
""",
            new { TenantId = _tenantContext.TenantId },
            session.Transaction);

        for (var i = 0; i < selectedPartNames.Count; i++)
        {
            var partName = selectedPartNames[i];
            var internalCode = $"CC-{timestamp}-{i + 1}";

            var partId = session.Connection.ExecuteScalar<int>(
                """
INSERT INTO dbo.Parts
(
    Name,
    InternalCode,
    Condition,
    IsActive,
    SalePrice,
    CostPrice,
    MinimumSellPrice,
    FastSalePrice,
    WholesalePrice,
    RecommendedPrice,
    MinStock,
    Currency,
    PricingStatus,
    CreatedAt,
    CreatedByUserId,
    TenantId
)
VALUES
(
    @Name,
    @InternalCode,
    2,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    'USD',
    'Manual',
    @CreatedAt,
    @CreatedByUserId,
    @TenantId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
                new
                {
                    Name = partName,
                    InternalCode = internalCode,
                    CreatedAt = now,
                    CreatedByUserId = createdByUserId,
                    TenantId = _tenantContext.TenantId
                },
                session.Transaction);

            if (warehouseId.HasValue)
            {
                session.Connection.Execute(
                    """
INSERT INTO dbo.Stock (PartId, WarehouseId, Quantity, ReservedQuantity)
VALUES (@PartId, @WarehouseId, 1, 0);
""",
                    new { PartId = partId, WarehouseId = warehouseId.Value },
                    session.Transaction);
            }
        }

        session.Commit();
    }
}
