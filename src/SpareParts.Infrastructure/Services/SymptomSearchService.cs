using SpareParts.Domain.SymptomSearch;

namespace SpareParts.Infrastructure.Services;

public sealed class SymptomSearchService
{
    private sealed record SymptomRule(
        string[] Keywords,
        string PartName,
        string PartNameAr,
        string Category,
        string[] SearchTerms);

    private static readonly SymptomRule[] Rules =
    [
        new(
            Keywords: ["click", "clicking", "tournant", "يطقطق"],
            PartName: "CV Joint / Driveshaft",
            PartNameAr: "مفصل التقاطع / أكسل",
            Category: "Drivetrain",
            SearchTerms: ["cv joint", "driveshaft", "axle"]),

        new(
            Keywords: ["squeak", "squeal", "grince", "يصرخ"],
            PartName: "Brake Pads / Rotors",
            PartNameAr: "تيل الفرامل",
            Category: "Brakes",
            SearchTerms: ["brake pads", "brake rotors", "disc brakes"]),

        new(
            Keywords: ["shake", "vibration", "vibre", "اهتزاز"],
            PartName: "Wheel Bearing / Tire / Brake Rotor",
            PartNameAr: "فرغل / رولمان العجلة",
            Category: "Suspension & Wheels",
            SearchTerms: ["wheel bearing", "tire balance", "brake rotor"]),

        new(
            Keywords: ["smoke", "fumée", "دخان"],
            PartName: "Engine Gasket / Piston Rings / Radiator Hose",
            PartNameAr: "حشوة المحرك",
            Category: "Engine",
            SearchTerms: ["head gasket", "piston rings", "radiator hose"]),

        new(
            Keywords: ["overheat", "chauffe", "يسخن"],
            PartName: "Thermostat / Radiator / Water Pump / Coolant Hose",
            PartNameAr: "ثرموستات",
            Category: "Cooling System",
            SearchTerms: ["thermostat", "radiator", "water pump", "coolant hose"]),

        new(
            Keywords: ["start", "démarrage", "لا يشتغل"],
            PartName: "Battery / Alternator / Starter Motor / Ignition Coil",
            PartNameAr: "بطارية",
            Category: "Electrical",
            SearchTerms: ["battery", "alternator", "starter motor", "ignition coil"]),

        new(
            Keywords: ["oil leak", "fuite d'huile", "تسريب زيت"],
            PartName: "Valve Cover Gasket / Oil Pan Gasket",
            PartNameAr: "حشوة غطاء المحرك",
            Category: "Engine",
            SearchTerms: ["valve cover gasket", "oil pan gasket", "oil seal"]),

        new(
            Keywords: ["rough idle", "يرتجف"],
            PartName: "Spark Plugs / Ignition Coil / Fuel Injector",
            PartNameAr: "بوجيهات",
            Category: "Engine",
            SearchTerms: ["spark plugs", "ignition coil", "fuel injector"]),

        new(
            Keywords: ["pull left", "pull right", "يجر"],
            PartName: "Tie Rod / Wheel Alignment / Brake Caliper",
            PartNameAr: "تيل التوجيه",
            Category: "Steering & Suspension",
            SearchTerms: ["tie rod", "wheel alignment", "brake caliper"]),

        new(
            Keywords: ["abs light", "abs", "ABS"],
            PartName: "ABS Sensor / ABS Pump",
            PartNameAr: "سنسور ABS",
            Category: "Brakes",
            SearchTerms: ["abs sensor", "abs pump", "wheel speed sensor"]),

        new(
            Keywords: ["check engine", "يشعل الداشبورد"],
            PartName: "OBD Scan Required",
            PartNameAr: "مطلوب قارئ أعطال",
            Category: "Diagnostics",
            SearchTerms: ["obd scan", "check engine", "diagnostic"]),

        new(
            Keywords: ["ac", "air conditioning", "climatisation", "مكيف"],
            PartName: "AC Compressor / AC Condenser / Refrigerant",
            PartNameAr: "كمبروسر المكيف",
            Category: "Air Conditioning",
            SearchTerms: ["ac compressor", "ac condenser", "refrigerant", "freon"]),

        new(
            Keywords: ["grinding", "يطحن", "grince"],
            PartName: "Brake Pads (Worn) / CV Joint / Wheel Bearing",
            PartNameAr: "تيل الفرامل",
            Category: "Brakes & Drivetrain",
            SearchTerms: ["brake pads worn", "cv joint", "wheel bearing"]),

        new(
            Keywords: ["window", "vitre", "نافذة"],
            PartName: "Window Regulator / Window Motor",
            PartNameAr: "موتور النافذة",
            Category: "Body & Electrical",
            SearchTerms: ["window regulator", "window motor", "power window"]),

        new(
            Keywords: ["door", "porte", "باب"],
            PartName: "Door Lock Actuator / Door Handle / Door Hinge",
            PartNameAr: "موتور القفل",
            Category: "Body",
            SearchTerms: ["door lock actuator", "door handle", "door hinge"]),
    ];

    public SymptomSearchResponse Diagnose(SymptomSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Symptoms))
        {
            throw new ValidationException("Symptoms are required.");
        }

        var input = request.Symptoms.ToLowerInvariant();

        var matched = new List<SymptomRule>();
        foreach (var rule in Rules)
        {
            foreach (var keyword in rule.Keywords)
            {
                if (input.Contains(keyword.ToLowerInvariant()))
                {
                    matched.Add(rule);
                    break;
                }
            }
        }

        var top3 = matched.Distinct().Take(3).ToList();

        var confidences = new[] { 100m, 85m, 70m };
        var results = top3
            .Select((rule, index) => new SymptomDiagnosisResult
            {
                SuggestedPartName = rule.PartName,
                SuggestedPartNameAr = rule.PartNameAr,
                ConfidencePercent = confidences[index],
                Category = rule.Category,
                SearchTerms = rule.SearchTerms.ToList(),
                Reasoning = $"Matched keyword pattern in symptom description."
            })
            .ToList();

        return new SymptomSearchResponse
        {
            Results = results,
            DiagnosticSummary = $"Found {results.Count} possible cause(s) for: {request.Symptoms}",
            Source = "static_rulebook_v1"
        };
    }
}
