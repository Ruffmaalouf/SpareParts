using System.Globalization;

namespace SpareParts.ImageEnrichment;

internal static class MissingPartsDetector
{
    private static readonly ExpectedPartDefinition[] ExpectedParts =
    [
        new("Complete engine", "Engine"),
        new("Engine control module", "Electronics"),
        new("Alternator", "Electrical"),
        new("Starter motor", "Electrical"),
        new("Air conditioning compressor", "Cooling", ["AC compressor", "A/C compressor"]),
        new("Radiator", "Cooling"),
        new("Cooling fan", "Cooling"),
        new("Intercooler", "Cooling"),
        new("Turbocharger", "Engine"),
        new("Fuel pump", "Fuel"),
        new("Exhaust manifold", "Exhaust"),
        new("Catalytic converter", "Exhaust"),
        new("Transmission gearbox", "Transmission"),
        new("Transfer case", "Transmission"),
        new("Differential", "Transmission"),
        new("Driveshaft", "Transmission"),
        new("Front left axle shaft", "Suspension"),
        new("Front right axle shaft", "Suspension"),
        new("Rear left axle shaft", "Suspension"),
        new("Rear right axle shaft", "Suspension"),
        new("Steering rack", "Steering"),
        new("Power steering pump", "Steering"),
        new("ABS pump module", "Brakes"),
        new("Brake master cylinder", "Brakes"),
        new("Front left strut", "Suspension"),
        new("Front right strut", "Suspension"),
        new("Rear left shock absorber", "Suspension"),
        new("Rear right shock absorber", "Suspension"),
        new("Front bumper", "Body"),
        new("Rear bumper", "Body"),
        new("Hood", "Body"),
        new("Trunk lid", "Body", ["liftgate", "tailgate", "hatch"]),
        new("Front left fender", "Body"),
        new("Front right fender", "Body"),
        new("Front left door", "Body"),
        new("Front right door", "Body"),
        new("Rear left door", "Body"),
        new("Rear right door", "Body"),
        new("Left side mirror", "Body"),
        new("Right side mirror", "Body"),
        new("Left headlight", "Lights"),
        new("Right headlight", "Lights"),
        new("Left tail light", "Lights"),
        new("Right tail light", "Lights"),
        new("Front grille", "Body"),
        new("Dashboard", "Interior"),
        new("Instrument cluster", "Interior"),
        new("Infotainment display", "Electronics"),
        new("Airbag control module", "Safety"),
        new("Driver airbag", "Safety"),
        new("Passenger airbag", "Safety"),
        new("Front left seat", "Interior"),
        new("Front right seat", "Interior"),
        new("Rear seat", "Interior"),
        new("Wheels set", "Wheels"),
        new("Parking sensor", "Sensors"),
        new("Oxygen sensor", "Sensors"),
        new("Mass air flow sensor", "Sensors")
    ];

    public static IReadOnlyList<MissingPartCandidate> Detect(
        IEnumerable<VehicleRecord> vehicles,
        IReadOnlyList<PartRecord> allParts,
        int? vehicleId)
    {
        var selectedVehicles = vehicles
            .Where(v => vehicleId is null || v.Id == vehicleId.Value)
            .OrderBy(v => v.Id)
            .ToList();

        var partsByVehicle = allParts
            .Where(p => p.UsedCarId is not null)
            .GroupBy(p => p.UsedCarId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var candidates = new List<MissingPartCandidate>();
        foreach (var vehicle in selectedVehicles)
        {
            var vehicleParts = partsByVehicle.TryGetValue(vehicle.Id, out var parts) ? parts : [];
            foreach (var expected in ExpectedPartsFor(vehicle))
            {
                if (HasExistingMatch(expected, vehicleParts))
                {
                    continue;
                }

                candidates.Add(new MissingPartCandidate
                {
                    DonorVehicleId = vehicle.Id,
                    Vehicle = vehicle.DisplayName,
                    ExpectedPartName = expected.Name,
                    Category = expected.Category,
                    ExpectedOemNumbers = null,
                    CompatibleYearRange = vehicle.Year is int year ? $"{year}" : null,
                    Engine = vehicle.EngineType,
                    BodyType = vehicle.BodyType,
                    Drivetrain = InferDrivetrain(vehicle),
                    SourceUrl = null,
                    ConfidenceScore = VehicleSpecificityScore(vehicle),
                    Status = "CandidateFromVehicleSpec",
                    Notes = "Generated from expected donor-vehicle dismantling checklist; candidate only, NeedsVerification, not sellable stock."
                });
            }
        }

        return candidates;
    }

    private static IEnumerable<ExpectedPartDefinition> ExpectedPartsFor(VehicleRecord vehicle)
    {
        foreach (var part in ExpectedParts)
        {
            if (part.Name.Contains("Turbocharger", StringComparison.OrdinalIgnoreCase) &&
                !LooksTurbocharged(vehicle))
            {
                continue;
            }

            if (part.Name.Contains("Intercooler", StringComparison.OrdinalIgnoreCase) &&
                !LooksTurbocharged(vehicle))
            {
                continue;
            }

            if (part.Name.Contains("Transfer case", StringComparison.OrdinalIgnoreCase) &&
                InferDrivetrain(vehicle) is null)
            {
                continue;
            }

            if ((vehicle.BodyType?.Contains("coupe", StringComparison.OrdinalIgnoreCase) ?? false) &&
                (part.Name.Contains("Rear left door", StringComparison.OrdinalIgnoreCase) ||
                 part.Name.Contains("Rear right door", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            yield return part;
        }
    }

    private static bool HasExistingMatch(ExpectedPartDefinition expected, IReadOnlyList<PartRecord> vehicleParts)
    {
        var expectedTokens = CoreTokens(expected.Name).ToList();
        if (expectedTokens.Count == 0)
        {
            return false;
        }

        foreach (var part in vehicleParts)
        {
            var searchable = $"{part.Name} {part.Category}".ToLowerInvariant();
            if (expected.Aliases?.Any(alias => AliasMatches(alias, searchable)) == true)
            {
                return true;
            }

            var matches = expectedTokens.Count(t => searchable.Contains(t, StringComparison.OrdinalIgnoreCase));
            if (matches >= Math.Max(1, expectedTokens.Count - 1))
            {
                return true;
            }

            if (expected.Category is not null &&
                part.Category?.Contains(expected.Category, StringComparison.OrdinalIgnoreCase) == true &&
                expectedTokens.Any(t => searchable.Contains(t, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AliasMatches(string alias, string searchable)
    {
        var tokens = CoreTokens(alias).ToList();
        return tokens.Count > 0 && tokens.All(t => searchable.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> CoreTokens(string value)
    {
        var directional = new HashSet<string>(["front", "rear", "left", "right", "set"], StringComparer.OrdinalIgnoreCase);
        return Program.TokenRegex().Matches(value.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length >= 3)
            .Where(t => !directional.Contains(t))
            .Where(t => !Program.StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? InferDrivetrain(VehicleRecord vehicle)
    {
        var text = $"{vehicle.Model} {vehicle.EngineType}".ToLowerInvariant();
        if (text.Contains("xdrive", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("awd", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("4matic", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("quattro", StringComparison.OrdinalIgnoreCase))
        {
            return "AWD";
        }

        return null;
    }

    private static bool LooksTurbocharged(VehicleRecord vehicle)
    {
        var text = $"{vehicle.Model} {vehicle.EngineType}".ToLowerInvariant();
        return text.Contains("turbo", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("2.0t", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("1.8t", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("3.0t", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("335", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("535", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("35i", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("35 ", StringComparison.OrdinalIgnoreCase);
    }

    private static double VehicleSpecificityScore(VehicleRecord vehicle)
    {
        var score = 40d;
        if (!string.IsNullOrWhiteSpace(vehicle.Make)) score += 8;
        if (!string.IsNullOrWhiteSpace(vehicle.Model)) score += 8;
        if (vehicle.Year is not null) score += 8;
        if (!string.IsNullOrWhiteSpace(vehicle.EngineType)) score += 8;
        if (!string.IsNullOrWhiteSpace(vehicle.BodyType)) score += 4;
        return Math.Round(Math.Clamp(score, 0, 80), 2);
    }

    private sealed record ExpectedPartDefinition(string Name, string Category, IReadOnlyList<string>? Aliases = null);
}
