using Dapper;
using SpareParts.Domain.Tenants;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class TenantsService
{
    private readonly ISqlConnectionFactory _factory;

    public TenantsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<TenantDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<TenantDto>(
            "SELECT Id, Name, Code, Domain, IsActive, CreatedAt, UpdatedAt FROM Tenants ORDER BY Name");
    }

    public TenantDto? GetById(int id)
    {
        using var conn = _factory.CreateConnection();
        return conn.QueryFirstOrDefault<TenantDto>(
            "SELECT Id, Name, Code, Domain, IsActive, CreatedAt, UpdatedAt FROM Tenants WHERE Id = @Id",
            new { Id = id });
    }

    public TenantDto? GetByCode(string code)
    {
        using var conn = _factory.CreateConnection();
        return conn.QueryFirstOrDefault<TenantDto>(
            "SELECT Id, Name, Code, Domain, IsActive, CreatedAt, UpdatedAt FROM Tenants WHERE Code = @Code",
            new { Code = code.Trim().ToLowerInvariant() });
    }

    public int Create(CreateTenantRequest request)
    {
        ValidateCode(request.Code);

        using var conn = _factory.CreateConnection();

        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Tenants WHERE Code = @Code",
            new { Code = NormalizeCode(request.Code) });

        if (exists > 0)
        {
            throw new ConflictException($"Tenant code '{request.Code}' is already in use.");
        }

        return conn.ExecuteScalar<int>(
            """
            INSERT INTO Tenants (Name, Code, Domain, IsActive, CreatedAt)
            VALUES (@Name, @Code, @Domain, 1, @Now);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                request.Name,
                Code = NormalizeCode(request.Code),
                Domain = NormalizeDomain(request.Domain),
                Now = DateTime.UtcNow
            });
    }

    public void Update(int id, UpdateTenantRequest request)
    {
        using var conn = _factory.CreateConnection();
        var rows = conn.Execute(
            """
            UPDATE Tenants
            SET Name = @Name, Domain = @Domain, IsActive = @IsActive, UpdatedAt = @Now
            WHERE Id = @Id
            """,
            new
            {
                Id = id,
                request.Name,
                Domain = NormalizeDomain(request.Domain),
                request.IsActive,
                Now = DateTime.UtcNow
            });

        if (rows == 0)
        {
            throw new NotFoundException("Tenant not found.");
        }
    }

    public bool ExistsAndActive(int tenantId)
    {
        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Tenants WHERE Id = @Id AND IsActive = 1",
            new { Id = tenantId }) > 0;
    }

    private static void ValidateCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ValidationException("Tenant code is required.");
        }

        var normalized = NormalizeCode(code);
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^[a-z0-9_-]{2,60}$"))
        {
            throw new ValidationException("Tenant code must be 2-60 characters and contain only lowercase letters, digits, hyphens, and underscores.");
        }
    }

    private static string NormalizeCode(string code) =>
        code.Trim().ToLowerInvariant();

    private static string? NormalizeDomain(string? domain) =>
        string.IsNullOrWhiteSpace(domain) ? null : domain.Trim().ToLowerInvariant();
}
