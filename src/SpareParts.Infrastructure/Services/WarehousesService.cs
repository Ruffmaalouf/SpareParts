using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class WarehousesService
{
    private readonly ISqlConnectionFactory _factory;

    public WarehousesService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<WarehouseDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<WarehouseDto>("SELECT Id, Name, Address, IsMain FROM Warehouses ORDER BY IsMain DESC, Name");
    }

    public int Create(CreateWarehouseRequest request)
    {
        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO Warehouses (Name, Address, IsMain, CreatedAt)
              VALUES (@Name, @Address, @IsMain, @Now);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new { request.Name, request.Address, request.IsMain, Now = DateTime.UtcNow });
    }

    public void Update(int id, CreateWarehouseRequest request)
    {
        using var conn = _factory.CreateConnection();
        var affected = conn.Execute(
            @"UPDATE Warehouses
              SET Name = @Name,
                  Address = @Address,
                  IsMain = @IsMain
              WHERE Id = @Id;",
            new { Id = id, request.Name, request.Address, request.IsMain });

        if (affected == 0)
        {
            throw new NotFoundException("Warehouse not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var affected = conn.Execute("DELETE FROM Warehouses WHERE Id = @Id;", new { Id = id });
        if (affected == 0)
        {
            throw new NotFoundException("Warehouse not found.");
        }
    }
}
