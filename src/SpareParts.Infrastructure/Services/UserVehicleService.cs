using Dapper;
using SpareParts.Domain.VehicleProfile;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class UserVehicleService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public UserVehicleService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<UserVehicleDto> GetAllForUser(int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<UserVehicleDto>(
            """
SELECT
    Id,
    TenantId,
    UserId,
    Nickname,
    VinNumber,
    Make,
    Model,
    Year,
    Trim,
    EngineCode,
    FuelType,
    Mileage,
    PhotoUrl,
    IsActive,
    IsDefault,
    CreatedAt
FROM dbo.UserVehicles
WHERE IsActive = 1
  AND UserId = @UserId
  AND (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY IsDefault DESC, CreatedAt DESC;
""",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction)
            .ToList();
    }

    public UserVehicleDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.QuerySingleOrDefault<UserVehicleDto>(
            """
SELECT
    Id,
    TenantId,
    UserId,
    Nickname,
    VinNumber,
    Make,
    Model,
    Year,
    Trim,
    EngineCode,
    FuelType,
    Mileage,
    PhotoUrl,
    IsActive,
    IsDefault,
    CreatedAt
FROM dbo.UserVehicles
WHERE Id = @Id
  AND IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public int Create(CreateUserVehicleRequest request, int userId, int? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Make))
            throw new ValidationException("Vehicle make is required.");
        if (string.IsNullOrWhiteSpace(request.Model))
            throw new ValidationException("Vehicle model is required.");
        if (request.Year < 1900 || request.Year > 2030)
            throw new ValidationException("Vehicle year must be between 1900 and 2030.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var existingCount = session.Connection.ExecuteScalar<int>(
            """
SELECT COUNT(1)
FROM dbo.UserVehicles
WHERE UserId = @UserId
  AND IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        var setAsDefault = request.SetAsDefault || existingCount == 0;

        if (setAsDefault)
        {
            session.Connection.Execute(
                """
UPDATE dbo.UserVehicles
SET IsDefault = 0
WHERE UserId = @UserId
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
                new { UserId = userId, TenantId = _tenantContext.TenantId },
                session.Transaction);
        }

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.UserVehicles
(
    TenantId,
    UserId,
    Nickname,
    VinNumber,
    Make,
    Model,
    Year,
    Trim,
    EngineCode,
    FuelType,
    Mileage,
    IsActive,
    IsDefault,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @TenantId,
    @UserId,
    @Nickname,
    @VinNumber,
    @Make,
    @Model,
    @Year,
    @Trim,
    @EngineCode,
    @FuelType,
    @Mileage,
    1,
    @IsDefault,
    @CreatedAt,
    @CreatedByUserId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                Nickname = request.Nickname,
                VinNumber = request.VinNumber,
                Make = request.Make,
                Model = request.Model,
                Year = request.Year,
                Trim = request.Trim,
                EngineCode = request.EngineCode,
                FuelType = request.FuelType,
                Mileage = request.Mileage,
                IsDefault = setAsDefault,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();
        return id;
    }

    public void SetDefault(int id, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var exists = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.UserVehicles
    WHERE Id = @Id AND UserId = @UserId AND IsActive = 1
    AND (@TenantId = 0 OR TenantId = @TenantId)
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { Id = id, UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (!exists)
            throw new NotFoundException("Vehicle not found.");

        session.Connection.Execute(
            """
UPDATE dbo.UserVehicles
SET IsDefault = 0
WHERE UserId = @UserId
  AND (@TenantId = 0 OR TenantId = @TenantId);

UPDATE dbo.UserVehicles
SET IsDefault = 1
WHERE Id = @Id;
""",
            new { Id = id, UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        session.Commit();
    }

    public void Deactivate(int id, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var exists = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.UserVehicles
    WHERE Id = @Id AND UserId = @UserId AND IsActive = 1
    AND (@TenantId = 0 OR TenantId = @TenantId)
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { Id = id, UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (!exists)
            throw new NotFoundException("Vehicle not found.");

        session.Connection.Execute(
            """
UPDATE dbo.UserVehicles
SET IsActive = 0, IsDefault = 0
WHERE Id = @Id;
""",
            new { Id = id },
            session.Transaction);

        session.Commit();
    }
}
