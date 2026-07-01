using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SpareParts.ImageEnrichment;

/// <summary>
/// Inserts missing parts into dbo.Parts for every vehicle that is missing a standard part type.
/// After insertion, runs OEM lookup and updates OEMNumber for high-confidence matches.
/// </summary>
internal sealed class MissingPartsInserter
{
    // Maps the MissingParts.cs category names → real dbo.Categories name fragments (fuzzy-matched at runtime)
    private static readonly Dictionary<string, string[]> CategorySearchTerms =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Engine"]       = ["Engine"],
            ["Electronics"]  = ["Electric", "Electronic"],
            ["Electrical"]   = ["Electric"],
            ["Cooling"]      = ["Cooling", "Heating"],
            ["Fuel"]         = ["Engine", "Fuel"],
            ["Exhaust"]      = ["Exhaust"],
            ["Transmission"] = ["Transmission"],
            ["Suspension"]   = ["Suspension"],
            ["Steering"]     = ["Steering"],
            ["Brakes"]       = ["Brake"],
            ["Body"]         = ["Body", "Exterior"],
            ["Lights"]       = ["Light", "Lamp"],
            ["Interior"]     = ["Interior"],
            ["Safety"]       = ["Safety"],
            ["Wheels"]       = ["Wheel", "Tyre", "Tire"],
            ["Sensors"]      = ["Sensor", "Electric"],
            ["Drivetrain"]   = ["Drivetrain", "Transmission"],
        };

    // Pre-defined slugs for known expected parts (matches ExpectedParts in MissingParts.cs)
    private static readonly Dictionary<string, string> PartSlugMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Complete engine"]            = "ENGINE",
            ["Engine control module"]      = "ECM",
            ["Alternator"]                 = "ALTERNATOR",
            ["Starter motor"]              = "STARTER",
            ["Air conditioning compressor"]= "AC-COMPRESSOR",
            ["Radiator"]                   = "RADIATOR",
            ["Cooling fan"]                = "COOLING-FAN",
            ["Intercooler"]                = "INTERCOOLER",
            ["Turbocharger"]               = "TURBOCHARGER",
            ["Fuel pump"]                  = "FUEL-PUMP",
            ["Exhaust manifold"]           = "EXHAUST-MANIFOLD",
            ["Catalytic converter"]        = "CAT-CONVERTER",
            ["Transmission gearbox"]       = "TRANSMISSION",
            ["Transfer case"]              = "TRANSFER-CASE",
            ["Differential"]               = "DIFFERENTIAL",
            ["Driveshaft"]                 = "DRIVESHAFT",
            ["Front left axle shaft"]      = "AXLE-FL",
            ["Front right axle shaft"]     = "AXLE-FR",
            ["Rear left axle shaft"]       = "AXLE-RL",
            ["Rear right axle shaft"]      = "AXLE-RR",
            ["Steering rack"]              = "STEERING-RACK",
            ["Power steering pump"]        = "STEERING-PUMP",
            ["ABS pump module"]            = "ABS-PUMP",
            ["Brake master cylinder"]      = "BRAKE-MC",
            ["Front left strut"]           = "STRUT-FL",
            ["Front right strut"]          = "STRUT-FR",
            ["Rear left shock absorber"]   = "SHOCK-RL",
            ["Rear right shock absorber"]  = "SHOCK-RR",
            ["Front bumper"]               = "BUMPER-FRONT",
            ["Rear bumper"]                = "BUMPER-REAR",
            ["Hood"]                       = "HOOD",
            ["Trunk lid"]                  = "TRUNK-LID",
            ["Front left fender"]          = "FENDER-FL",
            ["Front right fender"]         = "FENDER-FR",
            ["Front left door"]            = "DOOR-FL",
            ["Front right door"]           = "DOOR-FR",
            ["Rear left door"]             = "DOOR-RL",
            ["Rear right door"]            = "DOOR-RR",
            ["Left side mirror"]           = "MIRROR-L",
            ["Right side mirror"]          = "MIRROR-R",
            ["Left headlight"]             = "HEADLIGHT-L",
            ["Right headlight"]            = "HEADLIGHT-R",
            ["Left tail light"]            = "TAILLIGHT-L",
            ["Right tail light"]           = "TAILLIGHT-R",
            ["Front grille"]               = "GRILLE",
            ["Dashboard"]                  = "DASHBOARD",
            ["Instrument cluster"]         = "INST-CLUSTER",
            ["Infotainment display"]       = "INFOTAINMENT",
            ["Airbag control module"]      = "AIRBAG-MODULE",
            ["Driver airbag"]              = "AIRBAG-DRIVER",
            ["Passenger airbag"]           = "AIRBAG-PASS",
            ["Front left seat"]            = "SEAT-FL",
            ["Front right seat"]           = "SEAT-FR",
            ["Rear seat"]                  = "SEAT-REAR",
            ["Wheels set"]                 = "WHEELS-SET",
            ["Parking sensor"]             = "SENSOR-PARKING",
            ["Oxygen sensor"]              = "SENSOR-O2",
            ["Mass air flow sensor"]       = "SENSOR-MAF",
        };

    private readonly SqlConnection _conn;
    private readonly EnrichmentOptions _options;
    private readonly RunLogger _logger;
    private readonly OemEnricher _oemEnricher;

    // Loaded at startup
    private Dictionary<string, int> _categoryIds = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _existingCodes = new(StringComparer.OrdinalIgnoreCase);

    public MissingPartsInserter(SqlConnection conn, EnrichmentOptions options, RunLogger logger, OemEnricher oemEnricher)
    {
        _conn = conn;
        _options = options;
        _logger = logger;
        _oemEnricher = oemEnricher;
    }

    public async Task LoadAsync()
    {
        _categoryIds = await FetchCategoriesAsync();
        _existingCodes = await FetchExistingInternalCodesAsync();
        _logger.Info($"Categories loaded: {_categoryIds.Count}. Existing part codes: {_existingCodes.Count}.");
    }

    /// <summary>
    /// Inserts missing parts into dbo.Parts and optionally runs OEM lookup.
    /// Returns the list of inserted part records.
    /// </summary>
    public async Task<IReadOnlyList<InsertedPartResult>> InsertAsync(IReadOnlyList<MissingPartCandidate> candidates)
    {
        var results = new List<InsertedPartResult>();

        // Only process candidates with enough vehicle info (need year+make+model at minimum)
        var eligible = candidates.Where(c => c.ConfidenceScore >= 72).ToList();
        _logger.Info($"Missing part candidates eligible for auto-insert: {eligible.Count} / {candidates.Count}");

        foreach (var candidate in eligible)
        {
            var result = await InsertOneAsync(candidate);
            if (result is not null)
            {
                results.Add(result);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.DelayMilliseconds));
        }

        _logger.Info($"Parts inserted: {results.Count(r => r.Inserted)}. OEM numbers found: {results.Count(r => r.OemFound)}.");
        return results;
    }

    private async Task<InsertedPartResult?> InsertOneAsync(MissingPartCandidate candidate)
    {
        var result = new InsertedPartResult
        {
            DonorVehicleId = candidate.DonorVehicleId,
            Vehicle = candidate.Vehicle ?? "",
            ExpectedPartName = candidate.ExpectedPartName,
            Category = candidate.Category ?? ""
        };

        try
        {
            // Build part name: "{Vehicle Display} {Part Type Name}"
            var partName = BuildPartName(candidate);
            if (partName.Length > 200)
            {
                partName = partName[..200];
            }

            result.PartName = partName;

            // Generate unique internal code
            var internalCode = GenerateInternalCode(candidate.DonorVehicleId, candidate.ExpectedPartName);
            result.InternalCode = internalCode;

            // Resolve category
            var categoryId = ResolveCategoryId(candidate.Category ?? "");
            if (categoryId is null)
            {
                _logger.Warn($"  Skipping '{partName}' — could not resolve CategoryId for '{candidate.Category}'");
                result.SkipReason = $"Unknown category: {candidate.Category}";
                return result;
            }

            result.CategoryId = categoryId.Value;

            if (_options.DryRun)
            {
                _logger.Info($"  [DRY-RUN] Would insert: {partName} | Code: {internalCode} | Cat: {categoryId} | Vehicle: {candidate.DonorVehicleId}");
                result.Inserted = true;
                result.DryRun = true;
                return result;
            }

            // Insert the part
            var newPartId = await InsertPartAsync(partName, internalCode, categoryId.Value, candidate.DonorVehicleId);
            result.NewPartId = newPartId;
            result.Inserted = true;
            _existingCodes.Add(internalCode);
            _logger.Info($"  Inserted Part #{newPartId}: {partName} [{internalCode}]");

            // Mark candidate as inserted in VehicleExpectedPartCandidates
            await MarkCandidateInsertedAsync(candidate.DonorVehicleId, candidate.ExpectedPartName, newPartId);

            // Run OEM lookup
            if (_options.UpdateOem)
            {
                var partRecord = BuildPartRecordForOem(newPartId, partName, internalCode, candidate, categoryId.Value);
                var oemResult = await _oemEnricher.EnrichAsync(partRecord);
                result.OemStatus = oemResult.Status;

                if (oemResult.Status == "verified" &&
                    !string.IsNullOrWhiteSpace(oemResult.ProposedPrimaryOemNumber))
                {
                    await ApplyOemNumberAsync(newPartId, oemResult.ProposedPrimaryOemNumber);
                    result.OemFound = true;
                    result.OemNumber = oemResult.ProposedPrimaryOemNumber;
                    _logger.Info($"    OEM: {oemResult.ProposedPrimaryOemNumber} (score {oemResult.ConfidenceScore:0.0})");
                }
                else
                {
                    _logger.Info($"    OEM: not found ({oemResult.Status})");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"  Failed to insert '{candidate.ExpectedPartName}' for vehicle {candidate.DonorVehicleId}: {ex.Message}");
            result.SkipReason = ex.Message;
            return result;
        }
    }

    private static string BuildPartName(MissingPartCandidate candidate)
    {
        // Use the vehicle display name prefix + the expected part name
        // Vehicle is already "{Year} {Make} {Model} {Engine}" from VehicleRecord.DisplayName
        var vehiclePrefix = candidate.Vehicle?.Trim() ?? "";
        var partTypeName = candidate.ExpectedPartName.Trim();
        return string.IsNullOrWhiteSpace(vehiclePrefix)
            ? partTypeName
            : $"{vehiclePrefix} {partTypeName}";
    }

    private string GenerateInternalCode(int vehicleId, string partName)
    {
        var baseSlug = PartSlugMap.TryGetValue(partName, out var slug)
            ? slug
            : SlugifyPartName(partName);

        var baseCode = $"UC{vehicleId:D4}-{baseSlug}";

        // Ensure uniqueness — should rarely collide since we're adding missing parts
        if (!_existingCodes.Contains(baseCode))
        {
            return baseCode;
        }

        for (var i = 2; i <= 9; i++)
        {
            var candidate = $"{baseCode}-{i}";
            if (!_existingCodes.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseCode}-AUTO{vehicleId}";
    }

    private static string SlugifyPartName(string name)
    {
        var upper = name.ToUpperInvariant();
        // Keep only alphanumeric and spaces, collapse spaces to dashes
        var clean = Regex.Replace(upper, @"[^A-Z0-9\s]", "");
        var slug = Regex.Replace(clean.Trim(), @"\s+", "-");
        return slug.Length > 20 ? slug[..20] : slug;
    }

    private int? ResolveCategoryId(string categoryHint)
    {
        if (string.IsNullOrWhiteSpace(categoryHint))
        {
            return _categoryIds.Values.FirstOrDefault();
        }

        // Direct match first
        foreach (var (name, id) in _categoryIds)
        {
            if (name.Equals(categoryHint, StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
        }

        // Use the search terms map
        var searchTerms = CategorySearchTerms.TryGetValue(categoryHint, out var terms)
            ? terms
            : [categoryHint];

        foreach (var term in searchTerms)
        {
            foreach (var (name, id) in _categoryIds)
            {
                if (name.Contains(term, StringComparison.OrdinalIgnoreCase))
                {
                    return id;
                }
            }
        }

        // Last resort: partial match on the hint itself
        foreach (var (name, id) in _categoryIds)
        {
            if (name.Contains(categoryHint, StringComparison.OrdinalIgnoreCase) ||
                categoryHint.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                return id;
            }
        }

        return null;
    }

    private async Task<int> InsertPartAsync(string name, string internalCode, int categoryId, int vehicleId)
    {
        const string sql = """
INSERT INTO dbo.Parts
    (InternalCode, Name, Condition, CategoryId, CostPrice, SalePrice, Currency,
     MinStock, IsActive, UsedCarID, AllocatedCost, MinimumSellPrice, FastSalePrice,
     WholesalePrice, RecommendedPrice, PricingStatus, CostAllocationPercent,
     ImageUrls, Notes, CreatedAt)
OUTPUT INSERTED.Id
VALUES
    (@InternalCode, @Name, @Condition, @CategoryId, 0, 0, N'USD',
     0, 1, @VehicleId, 0, 0, 0,
     0, 0, N'Manual', 0,
     N'[]', @Notes, SYSUTCDATETIME());
""";
        await using var cmd = new SqlCommand(sql, _conn);
        Program.Add(cmd, "@InternalCode", internalCode);
        Program.Add(cmd, "@Name", name);
        Program.Add(cmd, "@Condition", 2);           // PartCondition.Used = 2
        Program.Add(cmd, "@CategoryId", categoryId);
        Program.Add(cmd, "@VehicleId", vehicleId);
        Program.Add(cmd, "@Notes", "Auto-inserted by missing-parts detector. Requires verification before selling.");
        var id = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(id, CultureInfo.InvariantCulture);
    }

    private async Task MarkCandidateInsertedAsync(int vehicleId, string expectedPartName, int newPartId)
    {
        const string sql = """
UPDATE dbo.VehicleExpectedPartCandidates
SET Status = N'inserted', Notes = ISNULL(Notes, '') + N' | Inserted as Part #' + CAST(@NewPartId AS NVARCHAR(20)),
    ModifiedAt = SYSUTCDATETIME()
WHERE DonorVehicleId = @VehicleId
  AND ExpectedPartName = @ExpectedPartName
  AND ApprovedByAdmin = 0;
""";
        await using var cmd = new SqlCommand(sql, _conn);
        Program.Add(cmd, "@NewPartId", newPartId);
        Program.Add(cmd, "@VehicleId", vehicleId);
        Program.Add(cmd, "@ExpectedPartName", expectedPartName);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task ApplyOemNumberAsync(int partId, string oemNumber)
    {
        const string sql = "UPDATE dbo.Parts SET OEMNumber = @OEMNumber, ModifiedAt = SYSUTCDATETIME() WHERE Id = @PartId;";
        await using var cmd = new SqlCommand(sql, _conn);
        Program.Add(cmd, "@PartId", partId);
        Program.Add(cmd, "@OEMNumber", oemNumber);
        await cmd.ExecuteNonQueryAsync();
    }

    private static PartRecord BuildPartRecordForOem(
        int partId,
        string partName,
        string internalCode,
        MissingPartCandidate candidate,
        int categoryId)
    {
        // Parse vehicle info from candidate
        var parts = (candidate.Vehicle ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int? year = parts.Length > 0 && int.TryParse(parts[0], out var y) ? y : null;
        var make = parts.Length > 1 ? parts[1] : null;
        var model = parts.Length > 2 ? string.Join(" ", parts[2..Math.Min(parts.Length, 4)]) : null;

        return new PartRecord
        {
            Id = partId,
            InternalCode = internalCode,
            Name = partName,
            Category = candidate.Category,
            CarMake = make,
            CarModel = model,
            CarYear = year,
            EngineType = candidate.Engine,
            UsedCarId = candidate.DonorVehicleId
        };
    }

    private async Task<Dictionary<string, int>> FetchCategoriesAsync()
    {
        const string sql = "SELECT Id, Name FROM dbo.Categories WHERE IsActive = 1 ORDER BY Name;";
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, _conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            dict[name] = id;
        }

        return dict;
    }

    private async Task<HashSet<string>> FetchExistingInternalCodesAsync()
    {
        const string sql = "SELECT DISTINCT InternalCode FROM dbo.Parts WHERE InternalCode IS NOT NULL;";
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, _conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var code = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(code))
            {
                set.Add(code);
            }
        }

        return set;
    }
}

internal sealed class InsertedPartResult
{
    public int DonorVehicleId { get; init; }
    public string Vehicle { get; init; } = "";
    public string ExpectedPartName { get; init; } = "";
    public string Category { get; init; } = "";
    public string PartName { get; set; } = "";
    public string InternalCode { get; set; } = "";
    public int CategoryId { get; set; }
    public int? NewPartId { get; set; }
    public bool Inserted { get; set; }
    public bool DryRun { get; set; }
    public string? SkipReason { get; set; }
    public bool OemFound { get; set; }
    public string? OemNumber { get; set; }
    public string? OemStatus { get; set; }
}
