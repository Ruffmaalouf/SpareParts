using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SpareParts.ImageEnrichment;

/// <summary>
/// For each donor vehicle, searches the internet (Bing + DuckDuckGo) against OEM parts catalog
/// sites (partsouq, realoem, 7zap, amayama, nemigaparts) to discover real parts with OEM numbers.
/// Every discovered part that does not already exist in dbo.Parts is inserted automatically.
/// </summary>
internal sealed partial class WebCatalogHarvester
{
    // ── Part categories to search across for every vehicle ──────────────────
    private static readonly (string Label, string[] SearchTerms, string DbCategory)[] SearchCategories =
    [
        ("engine",           ["engine assembly", "engine block", "engine oil", "timing chain", "crankshaft", "camshaft", "pistons", "oil pump", "water pump", "valve cover"],         "Engine"),
        ("alternator",       ["alternator", "starter motor", "battery", "voltage regulator"],                                                                                          "Electrical"),
        ("ecm ecu",          ["engine control module", "ECM ECU", "transmission control module", "BCM body control", "ABS module", "airbag SRS control module"],                     "Electrical"),
        ("radiator cooling", ["radiator", "cooling fan", "thermostat", "intercooler", "coolant reservoir", "heater core"],                                                            "Cooling & Heating"),
        ("transmission",     ["automatic transmission gearbox", "manual gearbox", "transfer case", "differential", "torque converter", "clutch kit"],                               "Transmission"),
        ("driveshaft axle",  ["driveshaft", "CV axle shaft", "CV joint", "half shaft"],                                                                                               "Drivetrain"),
        ("suspension",       ["front strut", "rear shock absorber", "control arm", "ball joint", "tie rod", "sway bar", "wheel bearing hub", "subframe"],                            "Suspension"),
        ("steering",         ["steering rack pinion", "power steering pump", "steering column", "steering wheel"],                                                                    "Steering"),
        ("brakes",           ["brake caliper", "brake rotor disc", "brake master cylinder", "brake booster", "ABS pump", "brake pad set", "parking brake"],                          "Brakes"),
        ("exhaust",          ["exhaust manifold", "catalytic converter", "muffler", "exhaust pipe", "oxygen sensor lambda", "EGR valve"],                                            "Exhaust System"),
        ("fuel system",      ["fuel pump", "fuel injector", "fuel rail", "fuel pressure regulator", "fuel tank"],                                                                     "Engine"),
        ("doors body",       ["front door assembly", "rear door assembly", "door panel", "door handle", "window regulator", "door lock actuator", "door hinge"],                    "Doors"),
        ("body exterior",    ["hood bonnet", "front bumper", "rear bumper", "front fender", "rear fender", "trunk lid", "roof panel", "quarter panel", "side skirt"],                "Body & Exterior"),
        ("lights",           ["headlight assembly", "tail light", "fog light", "turn signal", "daytime running light DRL"],                                                          "Lighting"),
        ("mirrors glass",    ["side mirror", "windshield glass", "rear window glass", "door glass", "mirror glass"],                                                                 "Glass & Mirrors"),
        ("interior",         ["dashboard", "instrument cluster", "center console", "door trim panel", "seat assembly", "airbag driver", "airbag passenger", "seatbelt"],            "Interior"),
        ("safety",           ["airbag module", "SRS sensor", "seatbelt pretensioner", "TPMS sensor"],                                                                               "Safety"),
        ("sensors",          ["mass airflow sensor MAF", "crankshaft position sensor", "camshaft sensor", "throttle position sensor", "temperature sensor", "speed sensor ABS"],    "Electrical"),
        ("turbo intake",     ["turbocharger", "supercharger", "intake manifold", "throttle body", "air filter box"],                                                                "Engine"),
        ("wheels",           ["alloy wheel rim", "wheel hub", "lug nut", "center cap"],                                                                                             "Wheels"),
        ("ac compressor",    ["AC compressor", "AC condenser", "AC evaporator", "blower motor HVAC", "AC expansion valve"],                                                        "Cooling & Heating"),
    ];

    // OEM catalog domains — results from these sites get high credibility
    private static readonly HashSet<string> TrustedOemDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "partsouq.com", "realoem.com", "7zap.com", "amayama.com", "nemigaparts.com",
        "oemcats.com", "partslink24.com", "autodoc.co.uk", "autodoc.de",
        "bmwpartsdepot.com", "ecstuning.com", "fcpeuro.com",
        "pelicanlparts.com", "rockauto.com", "meyle.com"
    };

    // Words to strip when building a part name from snippet context
    private static readonly HashSet<string> NameStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "OEM", "part", "parts", "number", "no", "genuine", "original", "used",
        "new", "buy", "shop", "catalog", "auto", "BMW", "Audi", "Mercedes",
        "Benz", "Nissan", "Honda", "Mitsubishi", "GMC", "for", "the", "and",
        "with", "fits", "fit", "ref", "code", "item", "SKU", "EAN"
    };

    private readonly ImageSearchClient _search;
    private readonly SqlConnection _conn;
    private readonly EnrichmentOptions _options;
    private readonly RunLogger _logger;

    // Loaded from DB: existing OEM numbers to prevent duplicates
    private HashSet<string> _existingOemNumbers = new(StringComparer.OrdinalIgnoreCase);

    public WebCatalogHarvester(
        ImageSearchClient search,
        SqlConnection conn,
        EnrichmentOptions options,
        RunLogger logger)
    {
        _search = search;
        _conn = conn;
        _options = options;
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        _existingOemNumbers = await FetchExistingOemNumbersAsync();
        _logger.Info($"Existing OEM numbers loaded: {_existingOemNumbers.Count}");
    }

    /// <summary>
    /// For each vehicle in the list, searches the internet for its parts catalog and inserts
    /// all discovered parts that are not already in the database.
    /// </summary>
    public async Task<HarvestSummary> HarvestAllVehiclesAsync(IReadOnlyList<VehicleRecord> vehicles)
    {
        var summary = new HarvestSummary();

        foreach (var vehicle in vehicles)
        {
            if (vehicle.Year is null || string.IsNullOrWhiteSpace(vehicle.Make) || string.IsNullOrWhiteSpace(vehicle.Model))
            {
                _logger.Warn($"Vehicle #{vehicle.Id} skipped — missing year/make/model.");
                summary.VehiclesSkipped++;
                continue;
            }

            _logger.Info($"Harvesting parts for vehicle #{vehicle.Id}: {vehicle.DisplayName}");
            var vehicleResult = await HarvestVehicleAsync(vehicle);
            summary.VehiclesProcessed++;
            summary.PartsDiscovered += vehicleResult.PartsDiscovered;
            summary.PartsInserted += vehicleResult.PartsInserted;
            summary.PartsSkipped += vehicleResult.PartsSkipped;
            _logger.Info($"  Vehicle #{vehicle.Id} done: {vehicleResult.PartsInserted} inserted, {vehicleResult.PartsSkipped} already existed, {vehicleResult.PartsDiscovered} discovered.");
        }

        return summary;
    }

    private async Task<VehicleHarvestResult> HarvestVehicleAsync(VehicleRecord vehicle)
    {
        var result = new VehicleHarvestResult { VehicleId = vehicle.Id };
        var discoveredParts = new List<DiscoveredPart>();

        // Search each category for this vehicle
        foreach (var (label, searchTerms, dbCategory) in SearchCategories)
        {
            // Skip turbo searches for non-turbocharged vehicles
            if (label.Contains("turbo", StringComparison.OrdinalIgnoreCase) && !LooksTurbocharged(vehicle))
            {
                continue;
            }

            // Use only the first (most specific) term per category — one image search query
            // is more effective than 3 text search queries here, and image search is heavier.
            var term = searchTerms[0];
            var categoryParts = await SearchPartTermAsync(vehicle, term, dbCategory);
            foreach (var part in categoryParts)
            {
                if (!discoveredParts.Any(d =>
                        d.OemNumber.Equals(part.OemNumber, StringComparison.OrdinalIgnoreCase)))
                {
                    discoveredParts.Add(part);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.DelayMilliseconds));
        }

        result.PartsDiscovered = discoveredParts.Count;

        // Also search for any part from a specific OEM catalog page (partsouq direct)
        var catalogParts = await SearchVehicleCatalogPageAsync(vehicle);
        foreach (var part in catalogParts)
        {
            if (!discoveredParts.Any(d => d.OemNumber.Equals(part.OemNumber, StringComparison.OrdinalIgnoreCase)))
            {
                discoveredParts.Add(part);
                result.PartsDiscovered++;
            }
        }

        // Insert all discovered parts that are not already in the DB
        if (!_options.DryRun)
        {
            foreach (var part in discoveredParts)
            {
                if (_existingOemNumbers.Contains(part.OemNumber))
                {
                    result.PartsSkipped++;
                    continue;
                }

                try
                {
                    await InsertDiscoveredPartAsync(part, vehicle);
                    _existingOemNumbers.Add(part.OemNumber);
                    result.PartsInserted++;
                }
                catch (Exception ex)
                {
                    _logger.Warn($"  Failed to insert '{part.PartName}' ({part.OemNumber}): {ex.Message}");
                    result.PartsSkipped++;
                }
            }
        }
        else
        {
            // Dry run: count what would be inserted
            foreach (var part in discoveredParts)
            {
                if (_existingOemNumbers.Contains(part.OemNumber))
                {
                    result.PartsSkipped++;
                }
                else
                {
                    _logger.Info($"  [DRY-RUN] Would insert: '{part.PartName}' OEM={part.OemNumber} ({part.Category})");
                    result.PartsInserted++;
                }
            }
        }

        return result;
    }

    private async Task<List<DiscoveredPart>> SearchPartTermAsync(
        VehicleRecord vehicle, string partTerm, string dbCategory)
    {
        var make = vehicle.Make!;
        var model = vehicle.Model!;
        var year = vehicle.Year!.Value.ToString(CultureInfo.InvariantCulture);

        var discovered = new List<DiscoveredPart>();

        // ── PRIMARY: Bing IMAGE search (proven to work in this environment) ─────────
        // Trusted OEM catalog sites (partsouq, realoem, autodoc, etc.) embed the OEM
        // part number in both the image title ("BMW Oil Filter 11427953125 | PartsOuq")
        // AND the source page URL ("/en/part/11427953125-bmw-oil-filter"), so we extract
        // OEM numbers from both fields.
        var imageQuery = $"{make} {model} {year} {partTerm} OEM genuine";
        var imageResults = await _search.SearchAsync(imageQuery);

        var trustedImages = imageResults
            .Where(img => img.SourcePageUrl is not null
                       && IsTrustedOemSource(img.SourcePageUrl))
            .ToList();

        if (trustedImages.Count > 0)
        {
            var fromImages = ExtractFromImageCandidates(trustedImages, vehicle, dbCategory);
            discovered.AddRange(fromImages);
            _logger.Info($"    [{partTerm}] images: {trustedImages.Count} trusted → {fromImages.Count} OEM hits");
        }

        // ── FALLBACK 1: Direct partsouq.com fetch ────────────────────────────────
        // Bypass Bing entirely and hit the OEM catalog directly. partsouq.com is
        // server-rendered so the HTML response contains part numbers in href paths.
        if (discovered.Count == 0)
        {
            discovered.AddRange(await FetchPartsOuqDirectAsync(vehicle, partTerm, dbCategory));
        }

        // ── FALLBACK 2: Bing text search targeted at partsouq.com ─────────────────
        // The site: operator forces Bing to return only partsouq.com pages whose
        // URLs contain OEM numbers as path segments (e.g. /en/part/11427953125-...).
        if (discovered.Count == 0)
        {
            var textQuery2 = $"site:partsouq.com {make} {model} {year} {partTerm}";
            var textResults2 = await _search.SearchBingOnlyAsync(textQuery2);
            var fromText2 = ExtractPartsFromResults(textResults2, vehicle, dbCategory);
            discovered.AddRange(fromText2);
            _logger.Info($"    [{partTerm}] site:partsouq Bing: {textResults2.Count} results → {fromText2.Count} OEM hits");
        }

        // ── FALLBACK 3: Bing text search targeted at realoem.com (BMW only) ────────
        if (discovered.Count == 0
            && vehicle.Make?.Equals("BMW", StringComparison.OrdinalIgnoreCase) == true)
        {
            var textQuery3 = $"site:realoem.com {make} {model} {year} {partTerm}";
            var textResults3 = await _search.SearchBingOnlyAsync(textQuery3);
            var fromText3 = ExtractPartsFromResults(textResults3, vehicle, dbCategory);
            discovered.AddRange(fromText3);
            _logger.Info($"    [{partTerm}] site:realoem Bing: {textResults3.Count} results → {fromText3.Count} OEM hits");
        }

        // ── FALLBACK 4: Generic Bing text search ─────────────────────────────────
        if (discovered.Count == 0)
        {
            var textQuery4 = $"{make} {model} {year} {partTerm} OEM part number partsouq OR realoem";
            var textResults4 = await _search.SearchBingOnlyAsync(textQuery4);
            var fromText4 = ExtractPartsFromResults(textResults4, vehicle, dbCategory);
            discovered.AddRange(fromText4);
            _logger.Info($"    [{partTerm}] generic Bing: {textResults4.Count} results → {fromText4.Count} OEM hits");
        }

        return discovered;
    }

    private async Task<List<DiscoveredPart>> SearchVehicleCatalogPageAsync(VehicleRecord vehicle)
    {
        var make = vehicle.Make!;
        var model = vehicle.Model!;
        var year = vehicle.Year!.Value.ToString(CultureInfo.InvariantCulture);

        // Use image search on partsouq/realoem directly; the source page URLs contain OEM numbers
        var catalogQuery = $"{make} {model} {year} OEM parts catalog genuine";
        var imageResults = await _search.SearchAsync(catalogQuery);

        var trustedImages = imageResults
            .Where(img => img.SourcePageUrl is not null && IsTrustedOemSource(img.SourcePageUrl))
            .ToList();

        return ExtractFromImageCandidates(trustedImages, vehicle, "General");
    }

    /// <summary>
    /// Converts Bing image search candidates (from trusted OEM domains) into
    /// DiscoveredPart objects by extracting OEM numbers from:
    /// (a) the image title — e.g. "BMW Oil Filter 11427953125 | PartsOuq"
    /// (b) the source page URL path — e.g. /en/part/11427953125-bmw-oil-filter
    /// </summary>
    private List<DiscoveredPart> ExtractFromImageCandidates(
        IEnumerable<ImageCandidate> candidates, VehicleRecord vehicle, string dbCategory)
    {
        var discovered = new List<DiscoveredPart>();
        foreach (var img in candidates)
        {
            var sourceUrl = img.SourcePageUrl ?? "";

            // (a) Extract from title + URL as a synthetic text result
            var syntheticResult = new TextSearchResult(
                img.Title, sourceUrl, img.Title, img.Provider);
            discovered.AddRange(ExtractPartsFromResults([syntheticResult], vehicle, dbCategory));

            // (b) Extract OEM tokens directly from the URL path segments
            discovered.AddRange(ExtractOemFromUrlPath(sourceUrl, img.Title, vehicle, dbCategory));
        }

        return discovered
            .DistinctBy(p => p.OemNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Looks for OEM-like tokens in a URL path split by / - _.
    /// E.g. /en/part/11427953125-BMW-oil-filter → extracts "11427953125".
    /// </summary>
    private List<DiscoveredPart> ExtractOemFromUrlPath(
        string url, string contextTitle, VehicleRecord vehicle, string dbCategory)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return [];
        var domain = DomainFromUrl(url);
        if (!TrustedOemDomains.Contains(domain)) return [];

        // Split the URL path on separators to get individual segments
        var segments = uri.AbsolutePath
            .Split(['/', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(s => s.Length is >= 6 and <= 18)
            .Where(s => s.Any(char.IsDigit) && !s.All(char.IsLetter))
            .Select(s => s.ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var discovered = new List<DiscoveredPart>();
        foreach (var segment in segments)
        {
            if (!IsValidOemNumber(segment, vehicle)) continue;

            var partName = ExtractPartNameFromContext(contextTitle, segment, vehicle, dbCategory);
            if (string.IsNullOrWhiteSpace(partName)) continue;

            var fakeResult = new TextSearchResult(contextTitle, url, contextTitle, "URL");
            var score = ScoreResult(fakeResult, vehicle, segment);
            if (score < 40) continue;

            discovered.Add(new DiscoveredPart
            {
                PartName = partName,
                OemNumber = segment,
                Category = dbCategory,
                SourceUrl = url,
                SourceDomain = domain,
                ConfidenceScore = score
            });
        }

        return discovered;
    }

    private List<DiscoveredPart> ExtractPartsFromResults(
        IReadOnlyList<TextSearchResult> results,
        VehicleRecord vehicle,
        string defaultCategory)
    {
        var discovered = new List<DiscoveredPart>();

        foreach (var result in results)
        {
            // Include the URL itself — OEM numbers often appear as path segments
            // (e.g. partsouq.com/en/part/11427953125-BMW-oil-filter)
            var searchableText = $"{result.Title} {result.Snippet} {result.Url}";
            var domain = DomainFromUrl(result.Url);

            foreach (Match match in OemLikeTokenRegex().Matches(searchableText.ToUpperInvariant()))
            {
                var token = CleanOemToken(match.Value);
                if (!IsValidOemNumber(token, vehicle))
                {
                    continue;
                }

                var partName = ExtractPartNameFromContext(searchableText, token, vehicle, defaultCategory);
                if (string.IsNullOrWhiteSpace(partName))
                {
                    continue;
                }

                // Score how trustworthy this result is
                var score = ScoreResult(result, vehicle, token);
                if (score < 40)
                {
                    continue;
                }

                discovered.Add(new DiscoveredPart
                {
                    PartName = partName,
                    OemNumber = token,
                    Category = defaultCategory,
                    SourceUrl = result.Url,
                    SourceDomain = domain,
                    ConfidenceScore = score
                });
            }
        }

        return discovered;
    }

    private static string ExtractPartNameFromContext(string text, string oemNumber, VehicleRecord vehicle, string fallbackCategory)
    {
        // Find the OEM number in the text (case-insensitive)
        var upperText = text.ToUpperInvariant();
        var oemIdx = upperText.IndexOf(oemNumber, StringComparison.OrdinalIgnoreCase);
        if (oemIdx < 0)
        {
            return BuildFallbackName(vehicle, fallbackCategory);
        }

        // Take up to 50 chars before the OEM number as context for the part name
        var contextStart = Math.Max(0, oemIdx - 80);
        var before = text[contextStart..oemIdx].Trim();

        // Split into words and filter
        var words = Program.TokenRegex().Matches(before)
            .Select(m => m.Value)
            .Where(w => w.Length >= 3)
            .Where(w => !int.TryParse(w, out _))
            .Where(w => !NameStopWords.Contains(w))
            .TakeLast(5)
            .ToList();

        if (words.Count < 2)
        {
            // Try after the OEM number
            var afterEnd = Math.Min(text.Length, oemIdx + oemNumber.Length + 80);
            var after = text[(oemIdx + oemNumber.Length)..afterEnd].Trim();
            words = Program.TokenRegex().Matches(after)
                .Select(m => m.Value)
                .Where(w => w.Length >= 3)
                .Where(w => !int.TryParse(w, out _))
                .Where(w => !NameStopWords.Contains(w))
                .Take(5)
                .ToList();
        }

        if (words.Count < 2)
        {
            return BuildFallbackName(vehicle, fallbackCategory);
        }

        // Capitalize each word
        var partTypeName = string.Join(" ", words.Select(w =>
            char.ToUpper(w[0]) + w[1..].ToLower(CultureInfo.InvariantCulture)));

        // Build full name: "{Year} {Make} {Model} {PartTypeName}"
        return $"{vehicle.DisplayName} {partTypeName}";
    }

    private static string BuildFallbackName(VehicleRecord vehicle, string category)
    {
        return $"{vehicle.DisplayName} {category} Part";
    }

    private static double ScoreResult(TextSearchResult result, VehicleRecord vehicle, string oemToken)
    {
        var text = $"{result.Title} {result.Snippet} {result.Url}".ToLowerInvariant();
        var score = 20d;

        var domain = DomainFromUrl(result.Url);
        if (TrustedOemDomains.Contains(domain))
        {
            score += 30;
        }

        if (!string.IsNullOrWhiteSpace(vehicle.Make) && text.Contains(vehicle.Make, StringComparison.OrdinalIgnoreCase))
        {
            score += 12;
        }

        if (!string.IsNullOrWhiteSpace(vehicle.Model) && text.Contains(vehicle.Model, StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        if (vehicle.Year is int yr && text.Contains(yr.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
        }

        if (text.Contains("oem", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("part number", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("genuine", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
        }

        // Penalize generic results (no vehicle match at all)
        if (!text.Contains(vehicle.Make ?? "", StringComparison.OrdinalIgnoreCase))
        {
            score -= 15;
        }

        return Math.Clamp(score, 0, 100);
    }

    private static bool IsValidOemNumber(string token, VehicleRecord vehicle)
    {
        if (token.Length < 5 || token.Length > 24)
        {
            return false;
        }

        // Must have at least one digit
        if (!token.Any(char.IsDigit))
        {
            return false;
        }

        // Not just a year
        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var num) &&
            num is >= 1900 and <= 2099)
        {
            return false;
        }

        // Not the make or model name
        var check = token.ToUpperInvariant();
        var rejects = new[] { vehicle.Make, vehicle.Model, vehicle.EngineType }
            .Where(s => !string.IsNullOrWhiteSpace(s) && s!.Length >= 4)
            .Select(s => s!.ToUpperInvariant());

        return !rejects.Any(r => check.Contains(r) || r.Contains(check));
    }

    private static string CleanOemToken(string raw)
    {
        var cleaned = Regex.Replace(raw, @"\s+", "", RegexOptions.CultureInvariant);
        return cleaned.Trim(' ', '.', ',', ':', ';', '(', ')', '[', ']');
    }

    private static bool IsTrustedOemSource(string url)
    {
        var domain = DomainFromUrl(url);
        return TrustedOemDomains.Any(d => domain.Contains(d, StringComparison.OrdinalIgnoreCase));
    }

    private static string DomainFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return "";
        }

        return uri.Host.ToLowerInvariant().TrimStart('w', '.');
    }

    private static bool LooksTurbocharged(VehicleRecord vehicle)
    {
        var text = $"{vehicle.Model} {vehicle.EngineType}".ToLowerInvariant();
        return text.Contains("turbo") || text.Contains("2.0t") || text.Contains("1.8t") ||
               text.Contains("3.0t") || text.Contains("335") || text.Contains("535") ||
               text.Contains("35i") || text.Contains("tdi") || text.Contains("tfsi");
    }

    /// <summary>
    /// Directly fetches partsouq.com's search page for a vehicle + part term and
    /// extracts OEM numbers from href URL path segments and data attributes.
    /// This bypasses Bing entirely for maximum reliability.
    /// </summary>
    private async Task<List<DiscoveredPart>> FetchPartsOuqDirectAsync(
        VehicleRecord vehicle, string partTerm, string dbCategory)
    {
        var make = vehicle.Make!;
        var model = vehicle.Model!;
        var year = vehicle.Year!.Value.ToString(CultureInfo.InvariantCulture);

        var query = Uri.EscapeDataString($"{make} {model} {year} {partTerm}");
        var searchUrl = $"https://partsouq.com/en/search?lang=en&q={query}";

        try
        {
            var html = await _search.FetchRawHtmlAsync(searchUrl);
            if (string.IsNullOrWhiteSpace(html))
            {
                _logger.Info($"    [{partTerm}] partsouq direct: empty/null response");
                return [];
            }

            var discovered = new List<DiscoveredPart>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // ── (a) OEM numbers embedded in href URL paths ────────────────────
            // e.g. href="/en/auto/BMW/3er/2010/11427953125-oil-filter"
            //      href="/en/catalog/11427953125"
            foreach (Match m in Regex.Matches(html,
                @"href=""([^""]*?/(\d{7,13})(?:[/""\-]|$))",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var oem = m.Groups[2].Value;
                if (!seen.Add(oem)) continue;
                if (!IsValidOemNumber(oem, vehicle)) continue;

                var idx = m.Index;
                var ctxStart = Math.Max(0, idx - 250);
                var ctxLen = Math.Min(500, html.Length - ctxStart);
                var context = StripHtmlTags(html.Substring(ctxStart, ctxLen));
                var partName = ExtractPartNameFromContext(context, oem, vehicle, dbCategory);

                discovered.Add(new DiscoveredPart
                {
                    PartName = string.IsNullOrWhiteSpace(partName)
                        ? $"{vehicle.DisplayName} {dbCategory}"
                        : partName,
                    OemNumber = oem,
                    Category = dbCategory,
                    SourceUrl = m.Groups[1].Value.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        ? m.Groups[1].Value
                        : $"https://partsouq.com{m.Groups[1].Value}",
                    SourceDomain = "partsouq.com",
                    ConfidenceScore = 72
                });
            }

            // ── (b) OEM numbers in data attributes ────────────────────────────
            // e.g. data-pno="11427953125"  data-oem="11427953125"
            foreach (Match m in Regex.Matches(html,
                @"data-[a-z\-]+=""(\d{7,13})""",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            {
                var oem = m.Groups[1].Value;
                if (!seen.Add(oem)) continue;
                if (!IsValidOemNumber(oem, vehicle)) continue;

                var idx = m.Index;
                var ctxStart = Math.Max(0, idx - 250);
                var ctxLen = Math.Min(500, html.Length - ctxStart);
                var context = StripHtmlTags(html.Substring(ctxStart, ctxLen));
                var partName = ExtractPartNameFromContext(context, oem, vehicle, dbCategory);

                discovered.Add(new DiscoveredPart
                {
                    PartName = string.IsNullOrWhiteSpace(partName)
                        ? $"{vehicle.DisplayName} {dbCategory}"
                        : partName,
                    OemNumber = oem,
                    Category = dbCategory,
                    SourceUrl = searchUrl,
                    SourceDomain = "partsouq.com",
                    ConfidenceScore = 68
                });
            }

            // ── (c) OEM numbers in standalone spans / text ───────────────────
            // e.g. <span class="pnum">11427953125</span>
            foreach (Match m in Regex.Matches(html,
                @">(\d{7,13})<",
                RegexOptions.CultureInvariant))
            {
                var oem = m.Groups[1].Value;
                if (!seen.Add(oem)) continue;
                if (!IsValidOemNumber(oem, vehicle)) continue;

                var idx = m.Index;
                var ctxStart = Math.Max(0, idx - 250);
                var ctxLen = Math.Min(500, html.Length - ctxStart);
                var context = StripHtmlTags(html.Substring(ctxStart, ctxLen));
                var partName = ExtractPartNameFromContext(context, oem, vehicle, dbCategory);

                discovered.Add(new DiscoveredPart
                {
                    PartName = string.IsNullOrWhiteSpace(partName)
                        ? $"{vehicle.DisplayName} {dbCategory}"
                        : partName,
                    OemNumber = oem,
                    Category = dbCategory,
                    SourceUrl = searchUrl,
                    SourceDomain = "partsouq.com",
                    ConfidenceScore = 65
                });
            }

            var unique = discovered
                .DistinctBy(p => p.OemNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.Info($"    [{partTerm}] partsouq direct: {unique.Count} OEM hits");
            return unique;
        }
        catch (Exception ex)
        {
            _logger.Warn($"    [{partTerm}] partsouq direct exception: {ex.Message}");
            return [];
        }
    }

    private static string StripHtmlTags(string html)
    {
        return Regex.Replace(html, "<[^>]+>", " ", RegexOptions.CultureInvariant)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase);
    }

    private async Task InsertDiscoveredPartAsync(DiscoveredPart part, VehicleRecord vehicle)
    {
        // Resolve category ID using the inserter's logic (reuse its LoadAsync result)
        const string catSql = """
SELECT TOP 1 Id FROM dbo.Categories
WHERE Name LIKE @Pattern AND IsActive = 1
ORDER BY CASE WHEN Name = @Exact THEN 0 ELSE 1 END, Name;
""";
        int categoryId;
        await using (var catCmd = new SqlCommand(catSql, _conn))
        {
            Program.Add(catCmd, "@Pattern", $"%{part.Category}%");
            Program.Add(catCmd, "@Exact", part.Category);
            var catResult = await catCmd.ExecuteScalarAsync();
            if (catResult is null or DBNull)
            {
                // Fallback to "Engine" (id 1 is usually the first category)
                categoryId = await GetFallbackCategoryIdAsync();
            }
            else
            {
                categoryId = Convert.ToInt32(catResult, CultureInfo.InvariantCulture);
            }
        }

        // Generate internal code
        var slug = part.OemNumber.Length > 12
            ? part.OemNumber[..12]
            : part.OemNumber;
        var internalCode = $"UC{vehicle.Id:D4}-OEM-{slug}";
        // Truncate to 60 chars
        if (internalCode.Length > 60)
        {
            internalCode = internalCode[..60];
        }

        // Truncate name to 200 chars
        var name = part.PartName.Length > 200 ? part.PartName[..200] : part.PartName;

        const string insertSql = """
INSERT INTO dbo.Parts
    (InternalCode, Name, OEMNumber, Condition, CategoryId, CostPrice, SalePrice, Currency,
     MinStock, IsActive, UsedCarID, AllocatedCost, MinimumSellPrice, FastSalePrice,
     WholesalePrice, RecommendedPrice, PricingStatus, CostAllocationPercent,
     ImageUrls, Notes, CreatedAt)
VALUES
    (@InternalCode, @Name, @OEMNumber, @Condition, @CategoryId, 0, 0, N'USD',
     0, 1, @VehicleId, 0, 0, 0,
     0, 0, N'Manual', 0,
     N'[]', @Notes, SYSUTCDATETIME());
""";
        await using var cmd = new SqlCommand(insertSql, _conn);
        Program.Add(cmd, "@InternalCode", internalCode);
        Program.Add(cmd, "@Name", name);
        Program.Add(cmd, "@OEMNumber", part.OemNumber);
        Program.Add(cmd, "@Condition", 2);  // Used
        Program.Add(cmd, "@CategoryId", categoryId);
        Program.Add(cmd, "@VehicleId", vehicle.Id);
        Program.Add(cmd, "@Notes",
            $"Web-harvested from {part.SourceDomain}. Confidence: {part.ConfidenceScore:0}. Requires verification before selling.");
        await cmd.ExecuteNonQueryAsync();

        _logger.Info($"  + Inserted: {name} (OEM: {part.OemNumber}) [{part.Category}] from {part.SourceDomain}");
    }

    private async Task<int> GetFallbackCategoryIdAsync()
    {
        const string sql = "SELECT TOP 1 Id FROM dbo.Categories WHERE IsActive = 1 ORDER BY Id;";
        await using var cmd = new SqlCommand(sql, _conn);
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? 1 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private async Task<HashSet<string>> FetchExistingOemNumbersAsync()
    {
        const string sql = """
SELECT DISTINCT OEMNumber FROM dbo.Parts
WHERE OEMNumber IS NOT NULL AND LTRIM(RTRIM(OEMNumber)) != '';
""";
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, _conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var val = reader.IsDBNull(0) ? null : reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(val))
            {
                set.Add(val.Trim());
            }
        }

        return set;
    }

    // Matches BMW-style (11427953125), Audi/VAG (8D0 615 601), hyphenated (12317506128), etc.
    [GeneratedRegex(@"\b[A-Z0-9]{2,}[-\s]?[A-Z0-9]{3,}(?:[-\s]?[A-Z0-9]{2,}){0,3}\b",
        RegexOptions.CultureInvariant)]
    private static partial Regex OemLikeTokenRegex();
}

// ── Result models ────────────────────────────────────────────────────────────

internal sealed class DiscoveredPart
{
    public string PartName { get; set; } = "";
    public string OemNumber { get; set; } = "";
    public string Category { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string SourceDomain { get; set; } = "";
    public double ConfidenceScore { get; set; }
}

internal sealed class HarvestSummary
{
    public int VehiclesProcessed { get; set; }
    public int VehiclesSkipped { get; set; }
    public int PartsDiscovered { get; set; }
    public int PartsInserted { get; set; }
    public int PartsSkipped { get; set; }
}

internal sealed class VehicleHarvestResult
{
    public int VehicleId { get; init; }
    public int PartsDiscovered { get; set; }
    public int PartsInserted { get; set; }
    public int PartsSkipped { get; set; }
}
