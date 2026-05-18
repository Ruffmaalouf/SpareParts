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
        return conn.Query<WarehouseDto>("SELECT Id, Name, Barcode, Address, IsMain FROM Warehouses ORDER BY IsMain DESC, Name");
    }

    public int Create(CreateWarehouseRequest request)
    {
        using var conn = _factory.CreateConnection();
        var id = conn.ExecuteScalar<int>(
            @"INSERT INTO Warehouses (Name, Barcode, Address, IsMain, CreatedAt)
              VALUES (@Name, @Barcode, @Address, @IsMain, @Now);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                request.Name,
                Barcode = NormalizeOptional(request.Barcode),
                request.Address,
                request.IsMain,
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
              WHERE Id = @Id;",
            new
            {
                Id = id,
                request.Name,
                Barcode = NormalizeOptional(request.Barcode),
                request.Address,
                request.IsMain
            });

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

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
