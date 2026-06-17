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
            ("price_genius", "PriceGenius AI pricing suggestions"),
            ("part_dna", "PartDNA condition scanning"),
            ("part_passport", "PartPassport transaction photo history"),
            ("community_guard", "CommunityGuard fraud reporting"),
            ("live_inspection", "LiveInspection video inspection requests"),
            ("qr_tag", "QR tag generation for parts"),
            ("part_genealogy", "PartGenealogy provenance timeline"),
            ("dismantler_forecast", "DismantlerForecast part-out value estimator"),
            ("regional_demand", "RegionalDemand map and insights"),
            ("mechanic_trust", "MechanicTrust verified mechanic profiles"),
            ("new_vs_used", "NewVsUsed pricing comparison"),
            ("yard_tour", "Live Yard Tour scheduling"),
            ("negotiation_bot", "AI Negotiation Bot"),
            ("instant_offer", "InstantOffer buy leads"),
            ("part_insurance", "PartInsurance add-on"),
            ("kareem_chat", "Kareem AI concierge"),
            ("ar_finder", "AR Parts Finder"),
            ("price_report_b2b", "B2B Price Reports"),
            ("api_platform", "External API platform access")
        };

        foreach (var (code, description) in features)
        {
            conn.Execute("""
IF NOT EXISTS (SELECT 1 FROM dbo.PackageFeatures WHERE FeatureCode = @Code)
BEGIN
    INSERT INTO dbo.PackageFeatures (FeatureCode, FeatureDescription, IsEnabledForFree, IsEnabledForBasic, IsEnabledForPro, IsEnabledForEnterprise)
    VALUES (@Code, @Description, 0, 0, 1, 1);
END;
""", new { Code = code, Description = description });
        }
    }
}
