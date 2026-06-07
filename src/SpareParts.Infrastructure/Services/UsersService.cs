using BCrypt.Net;
using Dapper;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class UsersService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public UsersService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<UserDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<UserDto>(
            """
            SELECT u.Id,
                   u.Username,
                   u.FullName,
                   u.Email,
                   u.RoleId,
                   COALESCE(r.Name, 'User') AS Role,
                   u.IsActive,
                   u.LastLoginAt,
                   u.CreatedAt
            FROM Users u
            LEFT JOIN Roles r ON r.Id = u.RoleId
            WHERE (@TenantId = 0 OR u.TenantId = @TenantId)
            ORDER BY u.FullName
            """,
            new { TenantId = _tenantContext.TenantId });
    }

    public int Create(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim(), workFactor: 12);

        using var conn = _factory.CreateConnection();
        var roleId = ResolveRoleId(conn, request.RoleId);
        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Users (Username, FullName, Email, PasswordHash, RoleId, TenantId, IsActive, CreatedAt)
            VALUES (@Username, @FullName, @Email, @Hash, @RoleId, @TenantId, 1, @Now);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                request.Username,
                request.FullName,
                request.Email,
                Hash = hash,
                RoleId = roleId,
                TenantId = _tenantContext.TenantId > 0 ? (int?)_tenantContext.TenantId : null,
                Now = DateTime.UtcNow
            });
    }

    public void Update(int id, UpdateUserRequest request)
    {
        using var conn = _factory.CreateConnection();
        var roleId = ResolveRoleId(conn, request.RoleId);

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword.Trim(), workFactor: 12);
            var affectedRows = conn.Execute(
                """
                UPDATE Users SET FullName = @FullName, Email = @Email, RoleId = @RoleId,
                                 IsActive = @IsActive, PasswordHash = @Hash,
                                 ModifiedAt = @Now
                WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)
                """,
                new
                {
                    request.FullName,
                    request.Email,
                    RoleId = roleId,
                    request.IsActive,
                    Hash = hash,
                    Now = DateTime.UtcNow,
                    Id = id,
                    TenantId = _tenantContext.TenantId
                });

            if (affectedRows == 0)
            {
                throw new NotFoundException("User not found.");
            }

            return;
        }

        var updatedRows = conn.Execute(
            """
            UPDATE Users SET FullName = @FullName, Email = @Email, RoleId = @RoleId,
                             IsActive = @IsActive, ModifiedAt = @Now
            WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)
            """,
            new { request.FullName, request.Email, RoleId = roleId, request.IsActive, Now = DateTime.UtcNow, Id = id, TenantId = _tenantContext.TenantId });

        if (updatedRows == 0)
        {
            throw new NotFoundException("User not found.");
        }
    }

    public void Deactivate(int id)
    {
        using var conn = _factory.CreateConnection();
        var affectedRows = conn.Execute(
            "UPDATE Users SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)",
            new { Now = DateTime.UtcNow, Id = id, TenantId = _tenantContext.TenantId });

        if (affectedRows == 0)
        {
            throw new NotFoundException("User not found.");
        }
    }

    private static int ResolveRoleId(System.Data.IDbConnection conn, int? roleId)
    {
        if (roleId.HasValue && roleId.Value > 0)
        {
            var exists = conn.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM Roles WHERE Id = @RoleId AND IsActive = 1",
                new { RoleId = roleId.Value });
            if (exists == 0)
            {
                throw new ValidationException("Role ID was not found.");
            }

            return roleId.Value;
        }

        throw new ValidationException("Role ID is required.");
    }
}
