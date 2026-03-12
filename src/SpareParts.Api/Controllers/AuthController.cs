using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    // ── DTOs ─────────────────────────────────────────────────────────────────
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token    { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role     { get; set; } = string.Empty;
        public int    UserId   { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // ── Controller ────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly JwtSettings _jwt;

        public AuthController(ISqlConnectionFactory factory, JwtSettings jwt)
        {
            _factory = factory;
            _jwt     = jwt;
        }

        /// <summary>
        /// POST /api/auth/login
        /// Body: { "username": "admin", "password": "Admin@123" }
        /// Returns a signed JWT on success.
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password))
                return BadRequest("Username and password are required.");

            // 1. Fetch user from DB
            await using var conn = _factory.CreateConnection() as System.Data.SqlClient.SqlConnection
                ?? throw new InvalidOperationException("Cannot cast connection.");

            var user = await conn.QueryFirstOrDefaultAsync(
                @"SELECT Id, Username, FullName, Email, PasswordHash, Role, IsActive
                  FROM   Users
                  WHERE  Username = @Username AND IsActive = 1",
                new { Username = req.Username.Trim() });

            if (user == null)
                return Unauthorized("Invalid username or password.");

            // 2. Verify BCrypt password
            bool valid = BCrypt.Net.BCrypt.Verify(req.Password, (string)user.PasswordHash);
            if (!valid)
                return Unauthorized("Invalid username or password.");

            // 3. Update LastLoginAt
            await conn.ExecuteAsync(
                "UPDATE Users SET LastLoginAt = @Now WHERE Id = @Id",
                new { Now = DateTime.UtcNow, Id = (int)user.Id });

            // 4. Build JWT
            var expiry  = DateTime.UtcNow.AddHours(_jwt.ExpiryHours);
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  ((int)user.Id).ToString()),
                new Claim(JwtRegisteredClaimNames.Name, (string)user.FullName),
                new Claim(ClaimTypes.Role,              (string)user.Role),
                new Claim("username",                   (string)user.Username),
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
                FullName  = (string)user.FullName,
                Role      = (string)user.Role,
                UserId    = (int)user.Id,
                ExpiresAt = expiry
            });
        }

        /// <summary>
        /// GET /api/auth/me  — returns current user info from the JWT claims.
        /// </summary>
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public ActionResult GetMe()
        {
            return Ok(new
            {
                UserId   = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value,
                Role     = User.FindFirst(ClaimTypes.Role)?.Value
            });
        }
    }
}
