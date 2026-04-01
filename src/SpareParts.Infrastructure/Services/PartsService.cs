using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class PartsService
{
    private readonly ISqlConnectionFactory _factory;

    public PartsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public (IEnumerable<PartDto> Items, int TotalCount) GetAll(int page, int pageSize)
    {
        using var session = new DbSession(_factory);
        var repository = new PartsRepository(session);
        var projected = repository.GetAllActive().Select(p => new PartDto
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
            Currency = p.Currency,
            MinStock = p.MinStock,
            Notes = p.Notes,
            IsActive = p.IsActive
        }).ToList();

        var paged = projected.Skip((page - 1) * pageSize).Take(pageSize);
        return (paged, projected.Count);
    }

    public int Create(CreatePartRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new PartsRepository(session);
        var part = new Part
        {
            InternalCode = request.InternalCode,
            Barcode = request.Barcode,
            Name = request.Name,
            OEMNumber = request.OEMNumber,
            Condition = request.Condition,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            CostPrice = request.CostPrice,
            SalePrice = request.SalePrice,
            Currency = request.Currency,
            MinStock = request.MinStock,
            Notes = request.Notes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(part);
        session.Commit();
        return id;
    }

    public void Update(int id, CreatePartRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new PartsRepository(session);
        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Part not found.");
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
}
