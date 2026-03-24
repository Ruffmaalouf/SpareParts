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
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly JwtSettings _jwt;

        public AuthController(ISqlConnectionFactory factory, JwtSettings jwt)
        {
            _factory = factory;
            _jwt = jwt;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Username) ||
                string.IsNullOrWhiteSpace(req.Password))
            {
                return BadRequest("Username and password are required.");
            }

            using var conn = _factory.CreateConnection();
            var user = conn.QueryFirstOrDefault<UserRow>(
                "SELECT Id, Username, FullName, PasswordHash, Role, IsActive " +
                "FROM Users WHERE Username = @Username",
                new { req.Username });

            if (user == null || !user.IsActive)
            {
                return Unauthorized("Invalid username or password.");
            }

            bool valid;
            try
            {
                valid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
            }
            catch
            {
                return Unauthorized(
                    "Password hash format is invalid. " +
                    "Use GET /api/auth/hashpassword?plain=YourPassword to generate a valid hash, " +
                    "then UPDATE Users SET PasswordHash = '<hash>' WHERE Username = '<user>'.");
            }

            if (!valid)
            {
                return Unauthorized("Invalid username or password.");
            }

            conn.Execute(
                "UPDATE Users SET LastLoginAt = @Now WHERE Id = @Id",
                new { Now = DateTime.UtcNow, user.Id });

            var expiry = DateTime.UtcNow.AddHours(_jwt.ExpiryHours);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

            return Ok(new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                FullName = user.FullName,
                Role = user.Role,
                UserId = user.Id,
                ExpiresAt = expiry
            });
        }

        [HttpGet("me")]
        [Authorize]
        public ActionResult GetMe() => Ok(new
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            FullName = User.FindFirst(ClaimTypes.Name)?.Value,
            Role = User.FindFirst(ClaimTypes.Role)?.Value
        });

        [HttpGet("hashpassword")]
        [Authorize(Roles = "Admin")]
        public ActionResult HashPassword([FromQuery] string plain)
        {
            if (string.IsNullOrWhiteSpace(plain))
            {
                return BadRequest("?plain= is required");
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(plain.Trim(), workFactor: 12);
            return Ok(new
            {
                plain,
                hash,
                sqlUpdate = $"UPDATE Users SET PasswordHash = '{hash}' WHERE Username = 'yourusername';"
            });
        }
    }
}
