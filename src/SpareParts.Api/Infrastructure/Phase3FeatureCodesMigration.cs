using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class Phase3FeatureCodesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        var features = new[]
        {
            "price_genius",
            "part_dna",
            "part_passport",
            "community_guard",
            "live_inspection",
            "qr_tag",
            "part_genealogy",
            "dismantler_forecast",
            "regional_demand",
            "mechanic_trust",
            "new_vs_used",
            "yard_tour",
            "negotiation_bot",
            "instant_offer",
            "part_insurance",
            "kareem_chat",
            "ar_finder",
            "price_report_b2b",
            "api_platform"
        };

        foreach (var code in features)
        {
            conn.Execute("""
INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
SELECT p.Id,
       @Code,
       CASE WHEN p.Code IN ('PRO', 'ENTERPRISE') THEN 1 ELSE 0 END,
       SYSUTCDATETIME()
FROM dbo.PricingPackages p
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.PackageFeatures pf WHERE pf.PackageId = p.Id AND pf.FeatureCode = @Code
);
""", new { Code = code });
        }
    }
}
