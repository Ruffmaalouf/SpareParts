using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize]                          // any logged-in user can read
    public class RolesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public RolesController(ISqlConnectionFactory factory) => _factory = factory;

        // GET /api/roles — all active roles
        [HttpGet]
        public ActionResult<IEnumerable<RoleDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            var rows = conn.Query<RoleDto>(
                @"SELECT Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive
                  FROM   Roles
                  WHERE  IsActive = 1
                  ORDER  BY IsSystem DESC, Name");
            return Ok(rows);
        }

        // GET /api/roles/{id}
        [HttpGet("{id:int}")]
        public ActionResult<RoleDto> GetById(int id)
        {
            using var conn = _factory.CreateConnection();
            var row = conn.QueryFirstOrDefault<RoleDto>(
                "SELECT Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive FROM Roles WHERE Id = @Id",
                new { Id = id });
            return row == null ? NotFound() : Ok(row);
        }

        // POST /api/roles — Admin only
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult<RoleDto> Create([FromBody] CreateRoleRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest("Role name is required.");

            using var conn = _factory.CreateConnection();

            // Check uniqueness
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Roles WHERE Name = @Name", new { req.Name });
            if (exists > 0)
                return Conflict($"Role '{req.Name}' already exists.");

            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO Roles (Name, Description, BadgeColor, BadgeTextColor, IsSystem, CreatedAt)
                  VALUES (@Name, @Description, @BadgeColor, @BadgeTextColor, 0, SYSUTCDATETIME());
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { req.Name, req.Description, req.BadgeColor, req.BadgeTextColor });

            var created = conn.QueryFirstOrDefault<RoleDto>(
                "SELECT Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive FROM Roles WHERE Id = @Id",
                new { Id = id });

            return Ok(created);
        }

        // PUT /api/roles/{id} — Admin only, cannot rename system roles
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public ActionResult Update(int id, [FromBody] UpdateRoleRequest req)
        {
            using var conn = _factory.CreateConnection();
            var affected = conn.Execute(
                @"UPDATE Roles
                  SET    Description    = @Description,
                         BadgeColor     = @BadgeColor,
                         BadgeTextColor = @BadgeTextColor,
                         IsActive       = @IsActive,
                         ModifiedAt     = SYSUTCDATETIME()
                  WHERE  Id = @Id",
                new { req.Description, req.BadgeColor, req.BadgeTextColor, req.IsActive, Id = id });

            return affected == 0 ? NotFound() : NoContent();
        }

        // DELETE /api/roles/{id} — Admin only, system roles protected
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            using var conn = _factory.CreateConnection();

            var isSystem = conn.ExecuteScalar<bool>(
                "SELECT IsSystem FROM Roles WHERE Id = @Id", new { Id = id });
            if (isSystem)
                return BadRequest("Built-in system roles cannot be deleted.");

            // Check if any user is assigned this role
            var usersWithRole = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Users WHERE RoleId = @Id", new { Id = id });
            if (usersWithRole > 0)
                return BadRequest($"Cannot delete role — {usersWithRole} user(s) are assigned to it.");

            conn.Execute("UPDATE Roles SET IsActive = 0, ModifiedAt = SYSUTCDATETIME() WHERE Id = @Id",
                new { Id = id });
            return NoContent();
        }
    }
}
