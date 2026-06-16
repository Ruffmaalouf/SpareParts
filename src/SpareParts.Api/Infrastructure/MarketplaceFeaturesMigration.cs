using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class MarketplaceFeaturesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
-- VIN_AUTOPILOT: PRO and ENTERPRISE only
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'VIN_AUTOPILOT',
       CASE WHEN p.Code IN ('PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'VIN_AUTOPILOT'
);

-- NEEDBOARD_POST: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'NEEDBOARD_POST',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'NEEDBOARD_POST'
);

-- NEEDBOARD_OFFERS: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'NEEDBOARD_OFFERS',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'NEEDBOARD_OFFERS'
);

-- WATCHLIST: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'WATCHLIST',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'WATCHLIST'
);

-- SELLER_VERIFICATION: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'SELLER_VERIFICATION',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'SELLER_VERIFICATION'
);

-- CURRENCY_SHIELD: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'CURRENCY_SHIELD',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'CURRENCY_SHIELD'
);

-- REPUTATION_SCORE: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'REPUTATION_SCORE',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'REPUTATION_SCORE'
);

-- BULK_PHOTO_STUDIO: PRO and ENTERPRISE only
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'BULK_PHOTO_STUDIO',
       CASE WHEN p.Code IN ('PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'BULK_PHOTO_STUDIO'
);
""");
    }
}
