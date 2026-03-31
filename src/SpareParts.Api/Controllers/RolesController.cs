using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;
using System.Linq;

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

            conn.Execute(
                @"INSERT INTO RoleMenuAccess (RoleId, MenuId, CanView, CanEdit, CanModify, CanDelete)
                  SELECT @RoleId, m.Id, 0, 0, 0, 0
                  FROM AppMenus m
                  WHERE m.IsActive = 1;",
                new { RoleId = id });

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

            var role = conn.QueryFirstOrDefault<(string Name, bool IsSystem)>(
                "SELECT Name, IsSystem FROM Roles WHERE Id = @Id", new { Id = id });
            if (string.IsNullOrWhiteSpace(role.Name))
                return NotFound();
            if (role.IsSystem)
                return BadRequest("Built-in system roles cannot be deleted.");

            // Check if any user is assigned this role
            var usersWithRole = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Users WHERE Role = @RoleName", new { RoleName = role.Name });
            if (usersWithRole > 0)
                return BadRequest($"Cannot delete role — {usersWithRole} user(s) are assigned to it.");

            conn.Execute("UPDATE Roles SET IsActive = 0, ModifiedAt = SYSUTCDATETIME() WHERE Id = @Id",
                new { Id = id });
            return NoContent();
        }

        // GET /api/roles/{id}/menu-access
        [HttpGet("{id:int}/menu-access")]
        public ActionResult<IEnumerable<RoleMenuAccessDto>> GetMenuAccessByRoleId(int id)
        {
            using var conn = _factory.CreateConnection();

            var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Id = @Id AND IsActive = 1", new { Id = id });
            if (exists == 0) return NotFound();

            var rows = conn.Query<RoleMenuAccessDto>(
                @"SELECT m.Id AS MenuId, m.MenuKey, m.MenuName, a.CanView, a.CanEdit, a.CanModify, a.CanDelete
                  FROM RoleMenuAccess a
                  INNER JOIN AppMenus m ON m.Id = a.MenuId
                  WHERE a.RoleId = @RoleId AND m.IsActive = 1
                  ORDER BY m.SortOrder, m.Id",
                new { RoleId = id }).ToList();

            return Ok(rows);
        }

        // GET /api/roles/by-name/{roleName}/menu-access
        [HttpGet("by-name/{roleName}/menu-access")]
        public ActionResult<IEnumerable<RoleMenuAccessDto>> GetMenuAccessByRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Role name is required.");

            using var conn = _factory.CreateConnection();

            var roleId = conn.QueryFirstOrDefault<int?>(
                "SELECT Id FROM Roles WHERE Name = @Name AND IsActive = 1",
                new { Name = roleName });

            if (roleId == null) return NotFound();

            var rows = conn.Query<RoleMenuAccessDto>(
                @"SELECT m.Id AS MenuId, m.MenuKey, m.MenuName, a.CanView, a.CanEdit, a.CanModify, a.CanDelete
                  FROM RoleMenuAccess a
                  INNER JOIN AppMenus m ON m.Id = a.MenuId
                  WHERE a.RoleId = @RoleId AND m.IsActive = 1
                  ORDER BY m.SortOrder, m.Id",
                new { RoleId = roleId.Value }).ToList();

            return Ok(rows);
        }

        // PUT /api/roles/{id}/menu-access
        [HttpPut("{id:int}/menu-access")]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateMenuAccess(int id, [FromBody] UpdateRoleMenuAccessRequest req)
        {
            if (req?.Items == null)
                return BadRequest("Access items are required.");

            using var conn = _factory.CreateConnection();

            var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Id = @Id AND IsActive = 1", new { Id = id });
            if (exists == 0) return NotFound();

            foreach (var item in req.Items)
            {
                conn.Execute(
                    @"MERGE dbo.RoleMenuAccess AS target
                      USING (SELECT @RoleId AS RoleId, @MenuId AS MenuId) AS source
                      ON target.RoleId = source.RoleId AND target.MenuId = source.MenuId
                      WHEN MATCHED THEN
                          UPDATE SET
                              CanView = @CanView,
                              CanEdit = @CanEdit,
                              CanModify = @CanModify,
                              CanDelete = @CanDelete,
                              ModifiedAt = SYSUTCDATETIME()
                      WHEN NOT MATCHED THEN
                          INSERT (RoleId, MenuId, CanView, CanEdit, CanModify, CanDelete, ModifiedAt)
                          VALUES (@RoleId, @MenuId, @CanView, @CanEdit, @CanModify, @CanDelete, SYSUTCDATETIME());",
                    new
                    {
                        RoleId = id,
                        item.MenuId,
                        item.CanView,
                        item.CanEdit,
                        item.CanModify,
                        item.CanDelete
                    });
            }

            return NoContent();
        }

    }
}
