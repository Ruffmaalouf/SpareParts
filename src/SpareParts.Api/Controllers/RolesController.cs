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
            EnsurePermissionsTable(conn);
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
            EnsurePermissionsTable(conn);
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
            EnsurePermissionsTable(conn);

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
                @"INSERT INTO RoleScreenPermissions
                    (RoleId, CanViewInvoiceSearch, CanViewManagementScreen, CanViewSupplierTab, CanEditSupplier, CanModifySupplier, CanDeleteSupplier)
                  VALUES
                    (@RoleId, 0, 0, 0, 0, 0, 0)",
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
            EnsurePermissionsTable(conn);
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
            EnsurePermissionsTable(conn);

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

        // GET /api/roles/{id}/permissions
        [HttpGet("{id:int}/permissions")]
        public ActionResult<RoleScreenPermissionsDto> GetPermissionsByRoleId(int id)
        {
            using var conn = _factory.CreateConnection();
            EnsurePermissionsTable(conn);

            var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Id = @Id AND IsActive = 1", new { Id = id });
            if (exists == 0) return NotFound();

            var row = conn.QueryFirstOrDefault<RoleScreenPermissionsDto>(
                @"SELECT RoleId, CanViewInvoiceSearch, CanViewManagementScreen, CanViewSupplierTab, CanEditSupplier, CanModifySupplier, CanDeleteSupplier
                  FROM RoleScreenPermissions
                  WHERE RoleId = @RoleId",
                new { RoleId = id });

            return row == null ? NotFound() : Ok(row);
        }

        // GET /api/roles/by-name/{roleName}/permissions
        [HttpGet("by-name/{roleName}/permissions")]
        public ActionResult<RoleScreenPermissionsDto> GetPermissionsByRoleName(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return BadRequest("Role name is required.");

            using var conn = _factory.CreateConnection();
            EnsurePermissionsTable(conn);

            var roleId = conn.QueryFirstOrDefault<int?>(
                "SELECT Id FROM Roles WHERE Name = @Name AND IsActive = 1",
                new { Name = roleName });

            if (roleId == null) return NotFound();

            var row = conn.QueryFirstOrDefault<RoleScreenPermissionsDto>(
                @"SELECT RoleId, CanViewInvoiceSearch, CanViewManagementScreen, CanViewSupplierTab, CanEditSupplier, CanModifySupplier, CanDeleteSupplier
                  FROM RoleScreenPermissions
                  WHERE RoleId = @RoleId",
                new { RoleId = roleId.Value });

            return row == null ? NotFound() : Ok(row);
        }

        // PUT /api/roles/{id}/permissions
        [HttpPut("{id:int}/permissions")]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdatePermissions(int id, [FromBody] UpdateRoleScreenPermissionsRequest req)
        {
            using var conn = _factory.CreateConnection();
            EnsurePermissionsTable(conn);

            var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Id = @Id AND IsActive = 1", new { Id = id });
            if (exists == 0) return NotFound();

            conn.Execute(
                @"UPDATE RoleScreenPermissions
                  SET CanViewInvoiceSearch = @CanViewInvoiceSearch,
                      CanViewManagementScreen = @CanViewManagementScreen,
                      CanViewSupplierTab = @CanViewSupplierTab,
                      CanEditSupplier = @CanEditSupplier,
                      CanModifySupplier = @CanModifySupplier,
                      CanDeleteSupplier = @CanDeleteSupplier,
                      ModifiedAt = SYSUTCDATETIME()
                  WHERE RoleId = @RoleId",
                new
                {
                    RoleId = id,
                    req.CanViewInvoiceSearch,
                    req.CanViewManagementScreen,
                    req.CanViewSupplierTab,
                    req.CanEditSupplier,
                    req.CanModifySupplier,
                    req.CanDeleteSupplier
                });

            return NoContent();
        }

        private static void EnsurePermissionsTable(System.Data.IDbConnection conn)
        {
            conn.Execute(
                @"
IF OBJECT_ID('dbo.RoleScreenPermissions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleScreenPermissions
    (
        RoleId INT NOT NULL PRIMARY KEY,
        CanViewInvoiceSearch BIT NOT NULL CONSTRAINT DF_RSP_CanViewInvoiceSearch DEFAULT (0),
        CanViewManagementScreen BIT NOT NULL CONSTRAINT DF_RSP_CanViewManagementScreen DEFAULT (0),
        CanViewSupplierTab BIT NOT NULL CONSTRAINT DF_RSP_CanViewSupplierTab DEFAULT (0),
        CanEditSupplier BIT NOT NULL CONSTRAINT DF_RSP_CanEditSupplier DEFAULT (0),
        CanModifySupplier BIT NOT NULL CONSTRAINT DF_RSP_CanModifySupplier DEFAULT (0),
        CanDeleteSupplier BIT NOT NULL CONSTRAINT DF_RSP_CanDeleteSupplier DEFAULT (0),
        ModifiedAt DATETIME2 NULL,
        CONSTRAINT FK_RoleScreenPermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE
    );
END;

INSERT INTO dbo.RoleScreenPermissions (RoleId, CanViewInvoiceSearch, CanViewManagementScreen, CanViewSupplierTab, CanEditSupplier, CanModifySupplier, CanDeleteSupplier)
SELECT r.Id,
       CASE WHEN r.Name IN ('Admin', 'Manager', 'Cashier') THEN 1 ELSE 0 END,
       CASE WHEN r.Name IN ('Admin', 'Manager') THEN 1 ELSE 0 END,
       CASE WHEN r.Name IN ('Admin', 'Manager') THEN 1 ELSE 0 END,
       CASE WHEN r.Name IN ('Admin', 'Manager') THEN 1 ELSE 0 END,
       CASE WHEN r.Name IN ('Admin', 'Manager') THEN 1 ELSE 0 END,
       CASE WHEN r.Name = 'Admin' THEN 1 ELSE 0 END
FROM dbo.Roles r
WHERE NOT EXISTS (SELECT 1 FROM dbo.RoleScreenPermissions p WHERE p.RoleId = r.Id);");
        }
    }
}
