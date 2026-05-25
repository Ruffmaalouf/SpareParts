using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class WebAppUserRoleMigration
{
    public const int RoleId = 4;

    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 4)
    BEGIN
        UPDATE dbo.Roles
        SET Name = N'Web App User',
            Description = COALESCE(NULLIF(Description, N''), N'Customer web/mobile shopping role'),
            BadgeColor = COALESCE(NULLIF(BadgeColor, N''), N'#2225D366'),
            BadgeTextColor = COALESCE(NULLIF(BadgeTextColor, N''), N'#FFFFFF'),
            IsSystem = 1,
            IsActive = 1,
            ModifiedAt = SYSUTCDATETIME()
        WHERE Id = 4;
    END
    ELSE
    BEGIN
        SET IDENTITY_INSERT dbo.Roles ON;

        INSERT INTO dbo.Roles
            (Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive, CreatedAt)
        VALUES
            (4, N'Web App User', N'Customer web/mobile shopping role', N'#2225D366', N'#FFFFFF', 1, 1, SYSUTCDATETIME());

        SET IDENTITY_INSERT dbo.Roles OFF;
    END
END;
""");
    }
}
