using Dapper;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class RolesService
{
    private readonly ISqlConnectionFactory _factory;

    public RolesService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<RoleDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<RoleDto>(
            @"SELECT Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive
              FROM Roles
              WHERE IsActive = 1
              ORDER BY IsSystem DESC, Name");
    }

    public RoleDto GetById(int id)
    {
        using var conn = _factory.CreateConnection();
        var role = conn.QueryFirstOrDefault<RoleDto>(
            "SELECT Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive FROM Roles WHERE Id = @Id",
            new { Id = id });

        return role ?? throw new NotFoundException("Role not found.");
    }

    public RoleDto Create(CreateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Role name is required.");
        }

        using var conn = _factory.CreateConnection();
        var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Name = @Name", new { request.Name });
        if (exists > 0)
        {
            throw new ConflictException($"Role '{request.Name}' already exists.");
        }

        var id = conn.ExecuteScalar<int>(
            @"INSERT INTO Roles (Name, Description, BadgeColor, BadgeTextColor, IsSystem, CreatedAt)
              VALUES (@Name, @Description, @BadgeColor, @BadgeTextColor, 0, SYSUTCDATETIME());
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new { request.Name, request.Description, request.BadgeColor, request.BadgeTextColor });

        conn.Execute(
            @"INSERT INTO RoleMenuAccess (RoleId, MenuId, CanView, CanEdit, CanModify, CanDelete)
              SELECT @RoleId, m.Id, 0, 0, 0, 0
              FROM AppMenus m
              WHERE m.IsActive = 1;",
            new { RoleId = id });

        return GetById(id);
    }

    public void Update(int id, UpdateRoleRequest request)
    {
        using var conn = _factory.CreateConnection();
        var affected = conn.Execute(
            @"UPDATE Roles
              SET Description = @Description,
                  BadgeColor = @BadgeColor,
                  BadgeTextColor = @BadgeTextColor,
                  IsActive = @IsActive,
                  ModifiedAt = SYSUTCDATETIME()
              WHERE Id = @Id",
            new { request.Description, request.BadgeColor, request.BadgeTextColor, request.IsActive, Id = id });

        if (affected == 0)
        {
            throw new NotFoundException("Role not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var role = conn.QueryFirstOrDefault<(string Name, bool IsSystem)>(
            "SELECT Name, IsSystem FROM Roles WHERE Id = @Id", new { Id = id });

        if (string.IsNullOrWhiteSpace(role.Name))
        {
            throw new NotFoundException("Role not found.");
        }

        if (role.IsSystem)
        {
            throw new ValidationException("Built-in system roles cannot be deleted.");
        }

        var usersWithRole = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Users WHERE Role = @RoleName", new { RoleName = role.Name });
        if (usersWithRole > 0)
        {
            throw new ValidationException($"Cannot delete role — {usersWithRole} user(s) are assigned to it.");
        }

        conn.Execute("UPDATE Roles SET IsActive = 0, ModifiedAt = SYSUTCDATETIME() WHERE Id = @Id", new { Id = id });
    }

    public IEnumerable<RoleMenuAccessDto> GetMenuAccessByRoleId(int id)
    {
        using var conn = _factory.CreateConnection();
        var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Id = @Id AND IsActive = 1", new { Id = id });
        if (exists == 0)
        {
            throw new NotFoundException("Role not found.");
        }

        return conn.Query<RoleMenuAccessDto>(
            @"SELECT m.Id AS MenuId, m.MenuKey, m.MenuName, a.CanView, a.CanEdit, a.CanModify, a.CanDelete
              FROM RoleMenuAccess a
              INNER JOIN AppMenus m ON m.Id = a.MenuId
              WHERE a.RoleId = @RoleId AND m.IsActive = 1
              ORDER BY m.SortOrder, m.Id",
            new { RoleId = id }).ToList();
    }

    public IEnumerable<RoleMenuAccessDto> GetMenuAccessByRoleName(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ValidationException("Role name is required.");
        }

        using var conn = _factory.CreateConnection();
        var roleId = conn.QueryFirstOrDefault<int?>(
            "SELECT Id FROM Roles WHERE Name = @Name AND IsActive = 1",
            new { Name = roleName });

        if (roleId == null)
        {
            throw new NotFoundException("Role not found.");
        }

        return conn.Query<RoleMenuAccessDto>(
            @"SELECT m.Id AS MenuId, m.MenuKey, m.MenuName, a.CanView, a.CanEdit, a.CanModify, a.CanDelete
              FROM RoleMenuAccess a
              INNER JOIN AppMenus m ON m.Id = a.MenuId
              WHERE a.RoleId = @RoleId AND m.IsActive = 1
              ORDER BY m.SortOrder, m.Id",
            new { RoleId = roleId.Value }).ToList();
    }

    public void UpdateMenuAccess(int id, UpdateRoleMenuAccessRequest request)
    {
        if (request?.Items == null)
        {
            throw new ValidationException("Access items are required.");
        }

        using var conn = _factory.CreateConnection();
        var exists = conn.ExecuteScalar<int>("SELECT COUNT(1) FROM Roles WHERE Id = @Id AND IsActive = 1", new { Id = id });
        if (exists == 0)
        {
            throw new NotFoundException("Role not found.");
        }

        foreach (var item in request.Items)
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
    }
}
