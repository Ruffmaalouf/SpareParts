using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class Phase2FeatureCodesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
-- SYMPTOM_SEARCH: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'SYMPTOM_SEARCH',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'SYMPTOM_SEARCH'
);

-- MECHANIC_DESK: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'MECHANIC_DESK',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'MECHANIC_DESK'
);

-- GARAGE_STOCK: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'GARAGE_STOCK',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'GARAGE_STOCK'
);

-- PART_RESERVE: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'PART_RESERVE',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'PART_RESERVE'
);

-- PART_REEL: PRO, ENTERPRISE only
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'PART_REEL',
       CASE WHEN p.Code IN ('PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'PART_REEL'
);

-- HALFCUT_SHOWCASE: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'HALFCUT_SHOWCASE',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'HALFCUT_SHOWCASE'
);

-- CAR_CRUSH: PRO, ENTERPRISE only
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'CAR_CRUSH',
       CASE WHEN p.Code IN ('PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'CAR_CRUSH'
);

-- ESCROW: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'ESCROW',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'ESCROW'
);

-- LISTING_BOOST: BASIC, PRO, ENTERPRISE
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'LISTING_BOOST',
       CASE WHEN p.Code IN ('BASIC', 'PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'LISTING_BOOST'
);

-- REFERRAL_ENGINE: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'REFERRAL_ENGINE',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'REFERRAL_ENGINE'
);

-- VOICE_SEARCH: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'VOICE_SEARCH',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'VOICE_SEARCH'
);

-- COMPATIBILITY_CHECK: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'COMPATIBILITY_CHECK',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'COMPATIBILITY_CHECK'
);

-- MARKET_PRICE_INDEX: all tiers
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'MARKET_PRICE_INDEX',
       1,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'MARKET_PRICE_INDEX'
);

-- WHATSAPP_CATALOG_EXPORT: PRO, ENTERPRISE only
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       'WHATSAPP_CATALOG_EXPORT',
       CASE WHEN p.Code IN ('PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = 'WHATSAPP_CATALOG_EXPORT'
);
""");
    }
}
