using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class WarehousesService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly ISubscriptionLimitService _subscriptionLimitService;

    public WarehousesService(ISqlConnectionFactory factory, ITenantContext tenantContext, ISubscriptionLimitService subscriptionLimitService)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _subscriptionLimitService = subscriptionLimitService;
    }

    public IEnumerable<WarehouseDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<WarehouseDto>(
            "SELECT Id, Name, Barcode, Address, IsMain FROM Warehouses WHERE (@TenantId = 0 OR TenantId = @TenantId) ORDER BY IsMain DESC, Name",
            new { TenantId = _tenantContext.TenantId });
    }

    public int Create(CreateWarehouseRequest request)
    {
        using var conn = _factory.CreateConnection();

        var branchesCount = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Warehouses WHERE (@TenantId = 0 OR TenantId = @TenantId)",
            new { TenantId = _tenantContext.TenantId });
        _subscriptionLimitService.EnsureWithinLimit(_tenantContext.TenantId, LimitCode.BranchesCount, branchesCount, "Warehouse/Branch Locations");

        var id = conn.ExecuteScalar<int>(
            @"INSERT INTO Warehouses (Name, Barcode, Address, IsMain, TenantId, CreatedAt)
              VALUES (@Name, @Barcode, @Address, @IsMain, @TenantId, @Now);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                request.Name,
                Barcode = NormalizeOptional(request.Barcode),
                request.Address,
                request.IsMain,
                TenantId = _tenantContext.TenantId > 0 ? (int?)_tenantContext.TenantId : null,
                Now = DateTime.UtcNow
            });

        conn.Execute(
            @"UPDATE Warehouses
              SET Barcode = @Barcode
              WHERE Id = @Id
                AND (Barcode IS NULL OR LTRIM(RTRIM(Barcode)) = N'');",
            new { Id = id, Barcode = $"WH-{id}" });

        return id;
    }

    public void Update(int id, CreateWarehouseRequest request)
    {
        using var conn = _factory.CreateConnection();
        var affected = conn.Execute(
            @"UPDATE Warehouses
              SET Name = @Name,
                  Barcode = @Barcode,
                  Address = @Address,
                  IsMain = @IsMain
              WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new
            {
                Id = id,
                request.Name,
                Barcode = NormalizeOptional(request.Barcode),
                request.Address,
                request.IsMain,
                TenantId = _tenantContext.TenantId
            });

        if (affected == 0)
        {
            throw new NotFoundException("Warehouse not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var affected = conn.Execute(
            "DELETE FROM Warehouses WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, TenantId = _tenantContext.TenantId });
        if (affected == 0)
        {
            throw new NotFoundException("Warehouse not found.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
