using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Api.Controllers;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Services;

public sealed class AuthService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly JwtSettings _jwt;

    public AuthService(ISqlConnectionFactory factory, JwtSettings jwt)
    {
        _factory = factory;
        _jwt = jwt;
    }

    public LoginResponse Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Username and password are required.");
        }

        using var conn = _factory.CreateConnection();
        var user = conn.QueryFirstOrDefault<UserRow>(
            "SELECT Id, Username, FullName, PasswordHash, Role, IsActive FROM Users WHERE Username = @Username",
            new { request.Username });

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        bool valid;
        try
        {
            valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }
        catch
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (!valid)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        conn.Execute(
            "UPDATE Users SET LastLoginAt = @Now WHERE Id = @Id",
            new { Now = DateTime.UtcNow, user.Id });

        var expiry = DateTime.UtcNow.AddHours(_jwt.ExpiryHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        var role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, fullName),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(ClaimTypes.Role, role),
            new Claim("username", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            FullName = fullName,
            Role = role,
            UserId = user.Id,
            ExpiresAt = expiry
        };
    }
}
