using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class SuppliersService
{
    private readonly ISqlConnectionFactory _factory;

    public SuppliersService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public (IEnumerable<SupplierDto> Items, int TotalCount) GetAll(int page, int pageSize)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        var projected = repository.GetAll().Select(s => new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            TaxNumber = s.TaxNumber,
            OpeningBalance = s.OpeningBalance
        }).ToList();

        var paged = projected.Skip((page - 1) * pageSize).Take(pageSize);
        return (paged, projected.Count);
    }

    public int Create(CreateSupplierRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        var supplier = new Supplier
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            TaxNumber = request.TaxNumber,
            OpeningBalance = request.OpeningBalance,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        var id = repository.Insert(supplier);
        session.Commit();
        return id;
    }

    public void Update(int id, CreateSupplierRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Supplier not found.");
        }

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory);
        var repository = new SuppliersRepository(session);
        if (!repository.Delete(id))
        {
            throw new NotFoundException("Supplier not found.");
        }

        session.Commit();
    }
}
