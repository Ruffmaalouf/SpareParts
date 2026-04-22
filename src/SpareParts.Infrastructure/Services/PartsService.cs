using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class PartsService
{
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

        var paged = projected.Skip((page - 1) * pageSize).Take(pageSize);
        return (paged, projected.Count);
    }

    public int Create(CreatePartRequest request, int userId)
    {
        ValidateUsedCar(request.UsedCarId);

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
        session.Commit();
        return id;
    }

    public void Update(int id, CreatePartRequest request, int userId)
    {
        ValidateUsedCar(request.UsedCarId);

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

    public void UpdateUsedCar(int id, int? usedCarId, int userId)
    {
        ValidateUsedCar(usedCarId);

        using var session = new DbSession(_factory);
        var repository = new PartsRepository(session);
        if (!repository.UpdateUsedCarId(id, usedCarId, userId))
        {
            throw new NotFoundException("Part not found.");
        }

        session.Commit();
    }

    public Task<GeneratePartNotesResponse> GenerateNotesAsync(
        GeneratePartNotesRequest request,
        CancellationToken cancellationToken = default)
        => _partNotesAiService.GenerateNotesAsync(request, cancellationToken);

    private void ValidateUsedCar(int? usedCarId)
    {
        if (usedCarId is not int validUsedCarId || validUsedCarId <= 0)
        {
            return;
        }

        using var session = new DbSession(_factory);
        var exists = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @Id;",
            new { Id = validUsedCarId },
            session.Transaction);
        if (exists == 0)
        {
            throw new ValidationException("Selected used car was not found.");
        }
    }
}
