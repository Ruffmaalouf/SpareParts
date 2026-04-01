using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class CustomersService
{
    private readonly ISqlConnectionFactory _factory;

    public CustomersService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public (IEnumerable<CustomerDto> Items, int TotalCount) GetAll(string? search, int page, int pageSize)
    {
        using var session = new DbSession(_factory);
        var repository = new CustomersRepository(session);
        var all = repository.GetAll();
        if (!string.IsNullOrWhiteSpace(search))
        {
            all = all.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var projected = all.Select(c => new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            TaxNumber = c.TaxNumber,
            OpeningBalance = c.OpeningBalance
        }).ToList();

        var paged = projected.Skip((page - 1) * pageSize).Take(pageSize);
        return (paged, projected.Count);
    }

    public int Create(CreateCustomerRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new CustomersRepository(session);
        var customer = new Customer
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

        var id = repository.Insert(customer);
        session.Commit();
        return id;
    }

    public void Update(int id, CreateCustomerRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var repository = new CustomersRepository(session);
        if (!repository.Update(id, request, userId))
        {
            throw new NotFoundException("Customer not found.");
        }

        session.Commit();
    }

    public void Delete(int id)
    {
        using var session = new DbSession(_factory);
        var repository = new CustomersRepository(session);
        if (!repository.Delete(id))
        {
            throw new NotFoundException("Customer not found.");
        }

        session.Commit();
    }
}
