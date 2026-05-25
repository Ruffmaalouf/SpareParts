using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UserRoleIdMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    DECLARE @SystemRoles TABLE
    (
        Id INT NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(255) NULL,
        BadgeColor NVARCHAR(20) NOT NULL,
        BadgeTextColor NVARCHAR(20) NOT NULL
    );

    INSERT INTO @SystemRoles (Id, Name, Description, BadgeColor, BadgeTextColor)
    VALUES
        (1, N'Admin', N'Full system access', N'#22FF5722', N'#FF7043'),
        (2, N'Manager', N'Operations access, no user management', N'#2200E5FF', N'#00E5FF'),
        (3, N'Cashier', N'POS sales only', N'#2244FF44', N'#44FF44'),
        (4, N'Web App User', N'Customer web/mobile shopping role', N'#2225D366', N'#FFFFFF');

    UPDATE r
    SET Name = s.Name,
        Description = COALESCE(NULLIF(r.Description, N''), s.Description),
        BadgeColor = COALESCE(NULLIF(r.BadgeColor, N''), s.BadgeColor),
        BadgeTextColor = COALESCE(NULLIF(r.BadgeTextColor, N''), s.BadgeTextColor),
        IsSystem = 1,
        IsActive = 1,
        ModifiedAt = SYSUTCDATETIME()
    FROM dbo.Roles r
    INNER JOIN @SystemRoles s ON s.Id = r.Id;

    IF EXISTS (SELECT 1 FROM @SystemRoles s WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = s.Id))
    BEGIN
        SET IDENTITY_INSERT dbo.Roles ON;

        INSERT INTO dbo.Roles
            (Id, Name, Description, BadgeColor, BadgeTextColor, IsSystem, IsActive, CreatedAt)
        SELECT s.Id, s.Name, s.Description, s.BadgeColor, s.BadgeTextColor, 1, 1, SYSUTCDATETIME()
        FROM @SystemRoles s
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = s.Id);

        SET IDENTITY_INSERT dbo.Roles OFF;
    END
END;

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Roles', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Users', 'RoleId') IS NULL
    BEGIN
        ALTER TABLE dbo.Users ADD RoleId INT NULL;
    END;

    IF COL_LENGTH('dbo.Users', 'Role') IS NOT NULL
    BEGIN
        EXEC(N'
UPDATE u
SET RoleId = r.Id
FROM dbo.Users u
INNER JOIN dbo.Roles r ON r.Name = CONVERT(NVARCHAR(100), u.Role)
WHERE u.RoleId IS NULL;');
    END;

    UPDATE u
    SET RoleId = 3
    FROM dbo.Users u
    WHERE u.RoleId IS NULL
       OR NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.Id = u.RoleId);

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE RoleId IS NULL)
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM sys.columns
            WHERE object_id = OBJECT_ID(N'dbo.Users')
              AND name = N'RoleId'
              AND is_nullable = 1
        )
        BEGIN
            ALTER TABLE dbo.Users ALTER COLUMN RoleId INT NOT NULL;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.default_constraints
            WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
              AND parent_column_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Users'), N'RoleId', 'ColumnId')
        )
        BEGIN
            ALTER TABLE dbo.Users ADD CONSTRAINT DF_Users_RoleId DEFAULT (3) FOR RoleId;
        END;

        IF NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID(N'dbo.Users')
              AND name = N'FK_Users_Roles_RoleId'
        )
        BEGIN
            ALTER TABLE dbo.Users WITH CHECK
            ADD CONSTRAINT FK_Users_Roles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id);
        END;
    END;

    IF COL_LENGTH('dbo.Users', 'Role') IS NOT NULL
    BEGIN
        DECLARE @RoleDefaultConstraint SYSNAME;
        DECLARE @DropRoleDefaultSql NVARCHAR(MAX);

        WHILE 1 = 1
        BEGIN
            SELECT TOP (1) @RoleDefaultConstraint = dc.name
            FROM sys.default_constraints dc
            INNER JOIN sys.columns c
                ON c.object_id = dc.parent_object_id
               AND c.column_id = dc.parent_column_id
            WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users')
              AND c.name = N'Role';

            IF @RoleDefaultConstraint IS NULL
            BEGIN
                BREAK;
            END;

            SET @DropRoleDefaultSql = N'ALTER TABLE dbo.Users DROP CONSTRAINT ' + QUOTENAME(@RoleDefaultConstraint);
            EXEC sp_executesql @DropRoleDefaultSql;
            SET @RoleDefaultConstraint = NULL;
        END;

        ALTER TABLE dbo.Users DROP COLUMN Role;
    END;
END;
""");
    }
}
