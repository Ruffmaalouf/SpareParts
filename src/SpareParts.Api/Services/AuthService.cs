using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Dapper;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Api.Hosting;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Middleware;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Services;

public sealed class AuthService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly JwtSettings _jwt;
    private readonly ExternalAuthSettings _externalAuth;
    private readonly HttpClient _httpClient;

    public AuthService(
        ISqlConnectionFactory factory,
        JwtSettings jwt,
        ExternalAuthSettings externalAuth,
        HttpClient httpClient)
    {
        _factory = factory;
        _jwt = jwt;
        _externalAuth = externalAuth;
        _httpClient = httpClient;
    }

    public LoginResponse Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Username and password are required.");
        }

        using var conn = _factory.CreateConnection();
        var user = conn.QueryFirstOrDefault<UserRow>(
            @"SELECT u.Id,
                     u.Username,
                     u.FullName,
                     u.PasswordHash,
                     u.RoleId,
                     u.IsActive,
                     u.TenantId,
                     COALESCE(t.Code, N'') AS TenantCode
              FROM Users u
              LEFT JOIN Tenants t ON t.Id = u.TenantId
              WHERE u.Username = @Username",
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

        return CreateLoginResponse(user);
    }

    public async Task<LoginResponse> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await VerifyExternalProfileAsync(request, cancellationToken);
        EnsureWebAppUserRoleExists();
        var username = BuildExternalUsername(profile.Provider, profile.ProviderUserId);

        using var conn = _factory.CreateConnection();
        var user = conn.QueryFirstOrDefault<UserRow>(
            @"SELECT u.Id,
                     u.Username,
                     u.FullName,
                     u.Email,
                     u.PasswordHash,
                     u.RoleId,
                     u.IsActive,
                     u.TenantId,
                     COALESCE(t.Code, N'') AS TenantCode
              FROM Users u
              LEFT JOIN Tenants t ON t.Id = u.TenantId
              WHERE u.Username = @Username",
            new { Username = username });

        if (user == null)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), workFactor: 12);
            var userId = conn.ExecuteScalar<int>(
                @"INSERT INTO Users (Username, FullName, Email, PasswordHash, RoleId, IsActive, CreatedAt, LastLoginAt)
                  VALUES (@Username, @FullName, @Email, @PasswordHash, @RoleId, 1, @Now, @Now);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    Username = username,
                    FullName = profile.FullName,
                    profile.Email,
                    PasswordHash = passwordHash,
                    RoleId = WebAppUserRoleMigration.RoleId,
                    Now = DateTime.UtcNow
                });

            user = new UserRow
            {
                Id = userId,
                Username = username,
                FullName = profile.FullName,
                Email = profile.Email,
                PasswordHash = passwordHash,
                RoleId = WebAppUserRoleMigration.RoleId,
                IsActive = true
            };
        }
        else
        {
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("This account is disabled.");
            }

            conn.Execute(
                @"UPDATE Users
                  SET FullName = @FullName,
                      Email = @Email,
                      RoleId = @RoleId,
                      LastLoginAt = @Now,
                      ModifiedAt = @Now
                  WHERE Id = @Id",
                new
                {
                    user.Id,
                    FullName = profile.FullName,
                    profile.Email,
                    RoleId = WebAppUserRoleMigration.RoleId,
                    Now = DateTime.UtcNow
                });

            user.FullName = profile.FullName;
            user.Email = profile.Email;
            user.RoleId = WebAppUserRoleMigration.RoleId;
        }

        return CreateLoginResponse(user);
    }

    private LoginResponse CreateLoginResponse(UserRow user)
    {
        var expiry = DateTime.UtcNow.AddHours(_jwt.ExpiryHours);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var fullName = string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName;
        var tenantId = user.TenantId ?? 0;
        var tenantCode = user.TenantCode ?? string.Empty;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, fullName),
            new Claim(ClaimTypes.Name, fullName),
            new Claim(AuthorizationPolicies.RoleIdClaimType, user.RoleId?.ToString() ?? string.Empty),
            new Claim("username", user.Username),
            new Claim(TenantResolutionMiddleware.TenantIdClaimType, tenantId.ToString()),
            new Claim(TenantResolutionMiddleware.TenantCodeClaimType, tenantCode),
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
            RoleId = user.RoleId,
            UserId = user.Id,
            ExpiresAt = expiry,
            TenantId = tenantId,
            TenantCode = tenantCode
        };
    }

    private async Task<ExternalProfile> VerifyExternalProfileAsync(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        var provider = (request.Provider ?? string.Empty).Trim().ToLowerInvariant();
        return provider switch
        {
            "google" => await VerifyGoogleAsync(request, cancellationToken),
            "facebook" or "fb" => await VerifyFacebookAsync(request, cancellationToken),
            _ => throw new ValidationException("Provider must be google or facebook.")
        };
    }

    private async Task<ExternalProfile> VerifyGoogleAsync(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_externalAuth.GoogleClientId))
        {
            throw new ValidationException("Google login is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            throw new ValidationException("Google id token is required.");
        }

        var url = $"https://oauth2.googleapis.com/tokeninfo?id_token={Uri.EscapeDataString(request.IdToken)}";
        var payload = await _httpClient.GetFromJsonAsync<GoogleTokenInfo>(url, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid Google login.");

        if (!string.Equals(payload.Audience, _externalAuth.GoogleClientId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(payload.Subject) ||
            !string.Equals(payload.EmailVerified, "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Invalid Google login.");
        }

        return new ExternalProfile(
            Provider: "google",
            ProviderUserId: payload.Subject,
            Email: NormalizeEmail(payload.Email) ?? $"{payload.Subject}@google.local",
            FullName: NormalizeDisplayName(payload.Name, payload.Email, "Google User"));
    }

    private async Task<ExternalProfile> VerifyFacebookAsync(ExternalLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_externalAuth.FacebookAppId) ||
            string.IsNullOrWhiteSpace(_externalAuth.FacebookAppSecret))
        {
            throw new ValidationException("Facebook login is not configured.");
        }

        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            throw new ValidationException("Facebook access token is required.");
        }

        var appToken = $"{_externalAuth.FacebookAppId}|{_externalAuth.FacebookAppSecret}";
        var debugUrl = "https://graph.facebook.com/debug_token" +
            $"?input_token={Uri.EscapeDataString(request.AccessToken)}" +
            $"&access_token={Uri.EscapeDataString(appToken)}";
        var debug = await _httpClient.GetFromJsonAsync<FacebookDebugTokenResponse>(debugUrl, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid Facebook login.");

        if (debug.Data == null ||
            !debug.Data.IsValid ||
            !string.Equals(debug.Data.AppId, _externalAuth.FacebookAppId, StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(debug.Data.UserId))
        {
            throw new UnauthorizedAccessException("Invalid Facebook login.");
        }

        var meUrl = "https://graph.facebook.com/me" +
            "?fields=id,name,email" +
            $"&access_token={Uri.EscapeDataString(request.AccessToken)}";
        var me = await _httpClient.GetFromJsonAsync<FacebookProfileResponse>(meUrl, cancellationToken)
            ?? throw new UnauthorizedAccessException("Invalid Facebook login.");

        if (!string.Equals(me.Id, debug.Data.UserId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Invalid Facebook login.");
        }

        return new ExternalProfile(
            Provider: "facebook",
            ProviderUserId: me.Id,
            Email: NormalizeEmail(me.Email) ?? $"{me.Id}@facebook.local",
            FullName: NormalizeDisplayName(me.Name, me.Email, "Facebook User"));
    }

    private void EnsureWebAppUserRoleExists()
    {
        using var conn = _factory.CreateConnection();
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Roles WHERE Id = @RoleId AND IsActive = 1",
            new { RoleId = WebAppUserRoleMigration.RoleId });

        if (exists == 0)
        {
            throw new ValidationException("Web App User role id 4 is not configured.");
        }
    }

    private static string BuildExternalUsername(string provider, string providerUserId)
    {
        var username = $"{provider}:{providerUserId}".Trim();
        if (username.Length <= 60)
        {
            return username;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(username))).ToLowerInvariant();
        return $"{provider}:{hash[..Math.Min(48, hash.Length)]}"[..60];
    }

    private static string NormalizeDisplayName(string? name, string? email, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            return Truncate(name.Trim(), 120);
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return Truncate(email.Trim(), 120);
        }

        return fallback;
    }

    private static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : Truncate(email.Trim(), 120);

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ExternalProfile(string Provider, string ProviderUserId, string? Email, string FullName);

    private sealed class GoogleTokenInfo
    {
        [JsonPropertyName("aud")]
        public string Audience { get; set; } = string.Empty;

        [JsonPropertyName("sub")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public string EmailVerified { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class FacebookDebugTokenResponse
    {
        [JsonPropertyName("data")]
        public FacebookDebugTokenData? Data { get; set; }
    }

    private sealed class FacebookDebugTokenData
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; } = string.Empty;

        [JsonPropertyName("is_valid")]
        public bool IsValid { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    private sealed class FacebookProfileResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
