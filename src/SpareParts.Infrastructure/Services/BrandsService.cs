using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class BrandsService
{
    private readonly ISqlConnectionFactory _factory;

    public BrandsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public (IEnumerable<BrandDto> Items, int TotalCount) GetAll(int page, int pageSize)
    {
        using var session = new DbSession(_factory);
        var repository = new BrandsRepository(session);
        var projected = repository.GetAll().Select(b => new BrandDto
        {
            Id = b.Id,
            Name = b.Name,
            IsActive = b.IsActive
        }).ToList();

        var paged = projected.Skip((page - 1) * pageSize).Take(pageSize);
        return (paged, projected.Count);
    }

    public int Create(CreateBrandRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new BrandsRepository(session);
        var brand = new Brand
        {
            Name = request.Name,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(brand);
        session.Commit();
        return id;
    }

    public void Update(int id, CreateBrandRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new BrandsRepository(session);
        if (!repository.Update(id, request.Name, request.IsActive, userId))
        {
            throw new NotFoundException("Brand not found.");
        }

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory);
        var repository = new BrandsRepository(session);
        if (!repository.Delete(id))
        {
            throw new NotFoundException("Brand not found.");
        }

        session.Commit();
    }
}
