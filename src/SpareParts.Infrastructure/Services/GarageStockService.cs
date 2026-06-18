using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.GarageStock;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class GarageStockService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public GarageStockService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<GarageStockItemDto> GetAll(bool? lowStockOnly = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var items = session.Connection.Query<GarageStockItemDto>(
            """
SELECT Id, TenantId, ItemName, Category, Barcode, OemNumber, Quantity, MinimumQuantity,
       PreferredSupplierId, Notes, IsActive, CreatedAt,
       CASE WHEN Quantity <= MinimumQuantity THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsLowStock
FROM dbo.GarageStockItems
WHERE IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId)
  AND (@LowStockOnly = 0 OR Quantity <= MinimumQuantity)
ORDER BY ItemName;
""",
            new { TenantId = _tenantContext.TenantId, LowStockOnly = lowStockOnly == true ? 1 : 0 },
            transaction: session.Transaction).ToList();
        session.Commit();
        return items;
    }

    public GarageStockItemDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var item = session.Connection.QueryFirstOrDefault<GarageStockItemDto>(
            """
SELECT Id, TenantId, ItemName, Category, Barcode, OemNumber, Quantity, MinimumQuantity,
       PreferredSupplierId, Notes, IsActive, CreatedAt,
       CASE WHEN Quantity <= MinimumQuantity THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsLowStock
FROM dbo.GarageStockItems
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);
        session.Commit();
        return item;
    }

    public int Create(CreateGarageStockItemRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
            throw new ValidationException("Item name is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.GarageStockItems
    (TenantId, ItemName, Category, Barcode, OemNumber, Quantity, MinimumQuantity,
     PreferredSupplierId, Notes, IsActive, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @ItemName, @Category, @Barcode, @OemNumber, @Quantity, @MinimumQuantity,
     @PreferredSupplierId, @Notes, 1, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                request.ItemName,
                request.Category,
                request.Barcode,
                request.OemNumber,
                request.Quantity,
                request.MinimumQuantity,
                request.PreferredSupplierId,
                request.Notes,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public void Update(int id, UpdateGarageStockItemRequest request, int modifiedByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.ItemName))
            throw new ValidationException("Item name is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.GarageStockItems
SET ItemName = @ItemName, Category = @Category, Barcode = @Barcode, OemNumber = @OemNumber,
    Quantity = @Quantity, MinimumQuantity = @MinimumQuantity, PreferredSupplierId = @PreferredSupplierId,
    Notes = @Notes, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new
            {
                Id = id,
                request.ItemName,
                request.Category,
                request.Barcode,
                request.OemNumber,
                request.Quantity,
                request.MinimumQuantity,
                request.PreferredSupplierId,
                request.Notes,
                TenantId = _tenantContext.TenantId,
                ModifiedByUserId = modifiedByUserId
            },
            transaction: session.Transaction);
        session.Commit();
    }

    public void UpdateQuantity(int id, int newQuantity, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.GarageStockItems
SET Quantity = @Quantity, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, Quantity = newQuantity, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    public void Deactivate(int id, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.GarageStockItems
SET IsActive = 0, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    public int GetLowStockCount()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var count = session.Connection.QuerySingle<int>(
            "SELECT COUNT(1) FROM dbo.GarageStockItems WHERE IsActive = 1 AND Quantity <= MinimumQuantity AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);
        session.Commit();
        return count;
    }
}
