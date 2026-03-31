using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class MenuAccessMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.AppMenus', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppMenus
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        MenuKey NVARCHAR(100) NOT NULL UNIQUE,
        MenuName NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_AppMenus_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AppMenus_IsActive DEFAULT (1),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AppMenus_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.RoleMenuAccess', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleMenuAccess
    (
        RoleId INT NOT NULL,
        MenuId INT NOT NULL,
        CanView BIT NOT NULL CONSTRAINT DF_RMA_CanView DEFAULT (0),
        CanEdit BIT NOT NULL CONSTRAINT DF_RMA_CanEdit DEFAULT (0),
        CanModify BIT NOT NULL CONSTRAINT DF_RMA_CanModify DEFAULT (0),
        CanDelete BIT NOT NULL CONSTRAINT DF_RMA_CanDelete DEFAULT (0),
        ModifiedAt DATETIME2 NULL,
        CONSTRAINT PK_RoleMenuAccess PRIMARY KEY (RoleId, MenuId),
        CONSTRAINT FK_RoleMenuAccess_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_RoleMenuAccess_Menus FOREIGN KEY (MenuId) REFERENCES dbo.AppMenus(Id) ON DELETE CASCADE
    );
END;

MERGE dbo.AppMenus AS target
USING (VALUES
    ('home_screen', 'Home Screen', 5),
    ('pos_screen', 'POS Screen', 8),
    ('car_selection_screen', 'Car Selection Screen', 9),
    ('part_selection_screen', 'Part Selection Screen', 9),
    ('invoice_search', 'Invoice Search', 10),
    ('purchases_screen', 'Purchases Screen', 15),
    ('stock_management_screen', 'Stock Management Screen', 18),
    ('management_screen', 'Management Screen', 20),
    ('supplier_tab', 'Supplier Tab', 30),
    ('currency_tab', 'Currency Tab', 31),
    ('transaction_types_tab', 'Transaction Types Tab', 32)
) AS source(MenuKey, MenuName, SortOrder)
ON target.MenuKey = source.MenuKey
WHEN MATCHED THEN
    UPDATE SET MenuName = source.MenuName, SortOrder = source.SortOrder, IsActive = 1
WHEN NOT MATCHED BY TARGET THEN
    INSERT (MenuKey, MenuName, SortOrder, IsActive) VALUES (source.MenuKey, source.MenuName, source.SortOrder, 1);

INSERT INTO dbo.RoleMenuAccess (RoleId, MenuId, CanView, CanEdit, CanModify, CanDelete)
SELECT r.Id,
       m.Id,
       CASE 
         WHEN m.MenuKey IN ('home_screen','pos_screen','car_selection_screen','part_selection_screen') AND r.Name IN ('Admin','Manager','Cashier') THEN 1
         WHEN m.MenuKey = 'invoice_search' AND r.Name IN ('Admin','Manager','Cashier') THEN 1
         WHEN m.MenuKey IN ('purchases_screen','stock_management_screen') AND r.Name IN ('Admin','Manager') THEN 1
         WHEN m.MenuKey IN ('management_screen','supplier_tab','currency_tab','transaction_types_tab') AND r.Name IN ('Admin','Manager') THEN 1
         ELSE 0
       END AS CanView,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Name IN ('Admin','Manager') THEN 1 ELSE 0 END AS CanEdit,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Name IN ('Admin','Manager') THEN 1 ELSE 0 END AS CanModify,
       CASE WHEN m.MenuKey = 'supplier_tab' AND r.Name = 'Admin' THEN 1 ELSE 0 END AS CanDelete
FROM dbo.Roles r
CROSS JOIN dbo.AppMenus m
WHERE m.IsActive = 1
  AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenuAccess a WHERE a.RoleId = r.Id AND a.MenuId = m.Id);");
    }
}
