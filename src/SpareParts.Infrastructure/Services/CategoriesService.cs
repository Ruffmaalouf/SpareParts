using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class CategoriesService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public CategoriesService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<CategoryDto> GetAll()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var repository = new CategoriesRepository(session);
        return repository.GetAll().Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            ParentId = c.ParentId
        }).ToList();
    }

    public int Create(CreateCategoryRequest request, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var repository = new CategoriesRepository(session);
        var category = new Category
        {
            Name = request.Name,
            ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(category);
        session.Commit();
        return id;
    }
}
