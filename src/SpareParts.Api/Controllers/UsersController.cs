using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;

        public UsersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<UserDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            return Ok(conn.Query<UserDto>(
                "SELECT Id, Username, FullName, Email, Role, IsActive, LastLoginAt, CreatedAt " +
                "FROM Users ORDER BY FullName"));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateUserRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Password is required.");
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(req.Password.Trim(), workFactor: 12);

            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
                  VALUES (@Username, @FullName, @Email, @Hash, @Role, 1, @Now);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    req.Username,
                    req.FullName,
                    req.Email,
                    Hash = hash,
                    req.Role,
                    Now = DateTime.UtcNow
                });
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public ActionResult Update(int id, [FromBody] UpdateUserRequest req)
        {
            using var conn = _factory.CreateConnection();

            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                var hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword.Trim(), workFactor: 12);
                conn.Execute(
                    @"UPDATE Users SET FullName = @FullName, Email = @Email, Role = @Role,
                                       IsActive = @IsActive, PasswordHash = @Hash,
                                       ModifiedAt = @Now
                      WHERE Id = @Id",
                    new
                    {
                        req.FullName,
                        req.Email,
                        req.Role,
                        req.IsActive,
                        Hash = hash,
                        Now = DateTime.UtcNow,
                        Id = id
                    });
            }
            else
            {
                conn.Execute(
                    @"UPDATE Users SET FullName = @FullName, Email = @Email, Role = @Role,
                                       IsActive = @IsActive, ModifiedAt = @Now
                      WHERE Id = @Id",
                    new { req.FullName, req.Email, req.Role, req.IsActive, Now = DateTime.UtcNow, Id = id });
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public ActionResult Deactivate(int id)
        {
            using var conn = _factory.CreateConnection();
            conn.Execute(
                "UPDATE Users SET IsActive = 0, ModifiedAt = @Now WHERE Id = @Id",
                new { Now = DateTime.UtcNow, Id = id });
            return NoContent();
        }
    }
}
