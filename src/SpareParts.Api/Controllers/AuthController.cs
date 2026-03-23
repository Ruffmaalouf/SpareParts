using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    // ══════════════════════════════════════════════════════════════════════════
    //  HEALTH — always anonymous, used by WPF login ping
    // ══════════════════════════════════════════════════════════════════════════
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        [HttpGet("api/health")]
        public ActionResult Get() => Ok(new { status = "ok", utc = DateTime.UtcNow });
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  AUTH CONTROLLER
    // ══════════════════════════════════════════════════════════════════════════
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly JwtSettings           _jwt;

        public AuthController(ISqlConnectionFactory factory, JwtSettings jwt)
        {
            _factory = factory;
            _jwt     = jwt;
        }

        /// <summary>POST /api/auth/login</summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username and password are required.");

            using var conn = _factory.CreateConnection();
            var user = conn.QueryFirstOrDefault<UserRow>(
                "SELECT Id, Username, FullName, PasswordHash, Role, IsActive " +
                "FROM Users WHERE Username = @Username",
                new { req.Username });

            if (user == null || !user.IsActive)
                return Unauthorized("Invalid username or password.");

            bool valid;
            try   { valid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash); }
            catch
            {
                return Unauthorized(
                    "Password hash format is invalid. " +
                    "Use GET /api/auth/hashpassword?plain=YourPassword to generate a valid hash, " +
                    "then UPDATE Users SET PasswordHash = '<hash>' WHERE Username = '<user>'.");
            }

            if (!valid)
                return Unauthorized("Invalid username or password.");

            conn.Execute(
                "UPDATE Users SET LastLoginAt = @Now WHERE Id = @Id",
                new { Now = DateTime.UtcNow, user.Id });

            var expiry = DateTime.UtcNow.AddHours(_jwt.ExpiryHours);
            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier,    user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(ClaimTypes.Name,              user.FullName),
                new Claim(ClaimTypes.Role,              user.Role),
                new Claim("username",                   user.Username),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer:             _jwt.Issuer,
                audience:           _jwt.Audience,
                claims:             claims,
                expires:            expiry,
                signingCredentials: creds);

            return Ok(new LoginResponse
            {
                Token     = new JwtSecurityTokenHandler().WriteToken(token),
                FullName  = user.FullName,
                Role      = user.Role,
                UserId    = user.Id,
                ExpiresAt = expiry
            });
        }

        /// <summary>GET /api/auth/me — current user from JWT</summary>
        [HttpGet("me")]
        [Authorize]
        public ActionResult GetMe() => Ok(new
        {
            UserId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            FullName = User.FindFirst(ClaimTypes.Name)?.Value,
            Role     = User.FindFirst(ClaimTypes.Role)?.Value
        });

        /// <summary>
        /// GET /api/auth/hashpassword?plain=Admin@123
        /// Generates a real BCrypt hash to paste into the DB.
        /// Admin-only utility endpoint for setup and support.
        /// </summary>
        [HttpGet("hashpassword")]
        [Authorize(Roles = "Admin")]
        public ActionResult HashPassword([FromQuery] string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
                return BadRequest("?plain= is required");

            var hash = BCrypt.Net.BCrypt.HashPassword(plain.Trim(), workFactor: 12);
            return Ok(new
            {
                plain,
                hash,
                sqlUpdate = $"UPDATE Users SET PasswordHash = '{hash}' WHERE Username = 'yourusername';"
            });
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  USERS CONTROLLER — full CRUD (Admin only)
    // ══════════════════════════════════════════════════════════════════════════
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
                return BadRequest("Password is required.");

            var hash = BCrypt.Net.BCrypt.HashPassword(req.Password.Trim(), workFactor: 12);

            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, IsActive, CreatedAt)
                  VALUES (@Username, @FullName, @Email, @Hash, @Role, 1, @Now);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { req.Username, req.FullName, req.Email,
                      Hash = hash, req.Role, Now = DateTime.UtcNow });
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
                    new { req.FullName, req.Email, req.Role, req.IsActive,
                          Hash = hash, Now = DateTime.UtcNow, Id = id });
            }
            else
            {
                conn.Execute(
                    @"UPDATE Users SET FullName = @FullName, Email = @Email, Role = @Role,
                                       IsActive = @IsActive, ModifiedAt = @Now
                      WHERE Id = @Id",
                    new { req.FullName, req.Email, req.Role, req.IsActive,
                          Now = DateTime.UtcNow, Id = id });
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

    // ── Internal Dapper row type (not exposed as DTO) ─────────────────────────
    internal class UserRow
    {
        public int    Id           { get; set; }
        public string Username     { get; set; } = string.Empty;
        public string FullName     { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role         { get; set; } = string.Empty;
        public bool   IsActive     { get; set; }
    }
}
