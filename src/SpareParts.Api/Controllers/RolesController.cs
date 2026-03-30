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
            EnsureMenuAccessTables(conn);
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
            EnsureMenuAccessTables(conn);
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
            EnsureMenuAccessTables(conn);

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
            EnsureMenuAccessTables(conn);
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
            EnsureMenuAccessTables(conn);

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
            EnsureMenuAccessTables(conn);

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
            EnsureMenuAccessTables(conn);

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
            EnsureMenuAccessTables(conn);

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

        private static void EnsureMenuAccessTables(System.Data.IDbConnection conn)
        {
            conn.Execute(
                @"
IF OBJECT_ID('dbo.AppMenus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppMenus
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MenuKey NVARCHAR(100) NOT NULL UNIQUE,
        MenuName NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_AppMenus_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AppMenus_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AppMenus_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.RoleMenuAccess', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMenuAccess
    (
        RoleId INT NOT NULL,
        MenuId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_RMA_CanView DEFAULT (0),
        CanEdit BIT NOT NULL CONSTRAINT DF_RMA_CanEdit DEFAULT (0),
        CanModify BIT NOT NULL CONSTRAINT DF_RMA_CanModify DEFAULT (0),
        CanDelete BIT NOT NULL CONSTRAINT DF_RMA_CanDelete DEFAULT (0),
        ModifiedAt DATETIME2 NULL,
        CONSTRAINT PK_RoleMenuAccess PRIMARY KEY (RoleId, MenuId),
        CONSTRAINT FK_RoleMenuAccess_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_RoleMenuAccess_Menus FOREIGN KEY (MenuId) REFERENCES dbo.AppMenus(Id) ON DELETE CASCADE
    );
END;

MERGE dbo.AppMenus AS target
USING (VALUES
    ('home_screen', 'Home Screen', 5),
    ('pos_screen', 'POS Screen', 8),
    ('car_selection_screen', 'Car Selection Screen', 9),
    ('part_selection_screen', 'Part Selection Screen', 9),
    ('invoice_search', 'Invoice Search', 10),
    ('purchases_screen', 'Purchases Screen', 15),
    ('stock_management_screen', 'Stock Management Screen', 18),
    ('management_screen', 'Management Screen', 20),
    ('supplier_tab', 'Supplier Tab', 30),
    ('currency_tab', 'Currency Tab', 31),
    ('transaction_types_tab', 'Transaction Types Tab', 32)
) AS source(MenuKey, MenuName, SortOrder)
ON target.MenuKey = source.MenuKey
WHEN MATCHED THEN
    UPDATE SET MenuName = source.MenuName, SortOrder = source.SortOrder, IsActive = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (MenuKey, MenuName, SortOrder, IsActive) VALUES (source.MenuKey, source.MenuName, source.SortOrder, 1);

INSERT INTO dbo.RoleMenuAccess (RoleId, MenuId, CanView, CanEdit, CanModify, CanDelete)
SELECT r.Id,
       m.Id,
       CASE 
         WHEN m.MenuKey IN ('home_screen','pos_screen','car_selection_screen','part_selection_screen') AND r.Name IN ('Admin','Manager','Cashier') THEN 1
         WHEN m.MenuKey = 'invoice_search' AND r.Name IN ('Admin','Manager','Cashier') THEN 1
         WHEN m.MenuKey IN ('purchases_screen','stock_management_screen') AND r.Name IN ('Admin','Manager') THEN 1
         WHEN m.MenuKey IN ('management_screen','supplier_tab','currency_tab','transaction_types_tab') AND r.Name IN ('Admin','Manager') THEN 1
         ELSE 0
       END AS CanView,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Name IN ('Admin','Manager') THEN 1 ELSE 0 END AS CanEdit,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Name IN ('Admin','Manager') THEN 1 ELSE 0 END AS CanModify,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Name = 'Admin' THEN 1 ELSE 0 END AS CanDelete
FROM dbo.Roles r
CROSS JOIN dbo.AppMenus m
WHERE m.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenuAccess a WHERE a.RoleId = r.Id AND a.MenuId = m.Id);");
        }
    }
}
