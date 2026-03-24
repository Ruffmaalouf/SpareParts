using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/warehouses")]
    [Authorize]
    public class WarehousesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public WarehousesController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<WarehouseDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            var rows = conn.Query<WarehouseDto>(
                "SELECT Id, Name, Address, IsMain FROM Warehouses ORDER BY IsMain DESC, Name");
            return Ok(rows);
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateWarehouseRequest req)
        {
            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO Warehouses (Name, Address, IsMain, CreatedAt)
                  VALUES (@Name, @Address, @IsMain, @Now);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { req.Name, req.Address, req.IsMain, Now = DateTime.UtcNow });
            return Ok(id);
        }
    }
}
