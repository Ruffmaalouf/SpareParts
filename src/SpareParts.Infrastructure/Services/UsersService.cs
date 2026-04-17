using BCrypt.Net;
using Dapper;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class UsersService
{
    private readonly ISqlConnectionFactory _factory;

    public UsersService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<UserDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<UserDto>(
            "SELECT Id, Username, FullName, Email, Role, IsActive, LastLoginAt, CreatedAt FROM Users ORDER BY FullName");
    }

    public int Create(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim(), workFactor: 12);

        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
              VALUES (@Username, @FullName, @Email, @Hash, @Role, 1, @Now);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                request.Username,
                request.FullName,
                request.Email,
                Hash = hash,
                request.Role,
                Now = DateTime.UtcNow
            });
    }

    public void Update(int id, UpdateUserRequest request)
    {
        using var conn = _factory.CreateConnection();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword.Trim(), workFactor: 12);
            var affectedRows = conn.Execute(
                @"UPDATE Users SET FullName = @FullName, Email = @Email, Role = @Role,
                                   IsActive = @IsActive, PasswordHash = @Hash,
                                   ModifiedAt = @Now
                  WHERE Id = @Id",
                new
                {
                    request.FullName,
                    request.Email,
                    request.Role,
                    request.IsActive,
                    Hash = hash,
                    Now = DateTime.UtcNow,
                    Id = id
                });

            if (affectedRows == 0)
            {
                throw new NotFoundException("User not found.");
            }

            return;
        }

        var updatedRows = conn.Execute(
            @"UPDATE Users SET FullName = @FullName, Email = @Email, Role = @Role,
                               IsActive = @IsActive, ModifiedAt = @Now
              WHERE Id = @Id",
            new { request.FullName, request.Email, request.Role, request.IsActive, Now = DateTime.UtcNow, Id = id });

        if (updatedRows == 0)
        {
            throw new NotFoundException("User not found.");
        }
    }

    public void Deactivate(int id)
    {
        using var conn = _factory.CreateConnection();
        var affectedRows = conn.Execute(
            "UPDATE Users SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
            new { Now = DateTime.UtcNow, Id = id });

        if (affectedRows == 0)
        {
            throw new NotFoundException("User not found.");
        }
    }
}
