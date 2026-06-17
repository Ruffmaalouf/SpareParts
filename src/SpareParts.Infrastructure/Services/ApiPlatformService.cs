using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using SpareParts.Domain.ApiPlatform;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class ApiPlatformService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ApiPlatformService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<ApiKeyDto> GetKeys()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<ApiKeyDto>(
            """
SELECT Id, TenantId, Name, KeyPrefix, Scopes, IsActive, ExpiresAt, CreatedAt
FROM dbo.ApiKeys
WHERE TenantId = @TenantId AND IsActive = 1
ORDER BY CreatedAt DESC;
""",
            new { TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public ApiKeyCreatedDto Create(CreateApiKeyRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Key name is required.");

        var plainKey = GeneratePlainKey();
        var prefix = plainKey[..8];
        var hash = HashKey(plainKey);
        var scopesJoined = request.Scopes.Count > 0 ? string.Join(",", request.Scopes) : null;
        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.ApiKeys (TenantId, Name, KeyHash, KeyPrefix, Scopes, IsActive, ExpiresAt, CreatedAt, CreatedByUserId)
VALUES (@TenantId, @Name, @KeyHash, @KeyPrefix, @Scopes, 1, @ExpiresAt, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                Name = request.Name,
                KeyHash = hash,
                KeyPrefix = prefix,
                Scopes = scopesJoined,
                ExpiresAt = request.ExpiresAt,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new ApiKeyCreatedDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            Name = request.Name,
            KeyPrefix = prefix,
            Scopes = scopesJoined,
            IsActive = true,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = now,
            PlainKey = plainKey
        };
    }

    public void Revoke(int keyId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            "UPDATE dbo.ApiKeys SET IsActive = 0 WHERE Id = @Id AND TenantId = @TenantId;",
            new { Id = keyId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        session.Commit();
    }

    private static string GeneratePlainKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return "sp_" + Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..40];
    }

    private static string HashKey(string plainKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plainKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
