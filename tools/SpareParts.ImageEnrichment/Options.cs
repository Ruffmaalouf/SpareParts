using System.Globalization;

namespace SpareParts.ImageEnrichment;

internal sealed class EnrichmentOptions
{
    public bool DryRun { get; init; } = true;
    public bool Yes { get; init; }
    public int? Limit { get; init; }
    public int? VehicleId { get; init; }
    public int? PartId { get; init; }
    public int BatchSize { get; init; } = 50;
    public bool Resume { get; init; }
    public bool ReplaceExisting { get; init; } = true;
    public bool ReprocessFailedOnly { get; init; }
    public bool ReprocessAll { get; init; }
    public bool ReportOnly { get; init; }
    public bool UpdateOem { get; init; } = true;
    public bool UpdateImages { get; init; } = true;
    public bool DetectMissingParts { get; init; } = true;
    public bool AutoInsertMissingParts { get; init; }
    public bool HarvestPartsFromWeb { get; init; }
    public bool SaveCandidates { get; init; } = true;
    public string MinConfidence { get; init; } = "high";
    public string ConnectionString { get; init; } = "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";
    public string? ApplyReviewCsv { get; init; }
    public string GoogleApiKey { get; init; } = "";
    public string GoogleCx { get; init; } = "";
    public int MaxQueriesPerPart { get; init; } = 6;
    public int MaxTextResultsPerQuery { get; init; } = 8;
    public int CandidateLimitPerPart { get; init; } = 3;
    public int DelayMilliseconds { get; init; } = 300;
    public int HttpTimeoutSeconds { get; init; } = 20;
    public bool AllowUnknownLicenseAutoUpdate { get; init; }
    public bool SearchReviewWithoutIdentifiers { get; init; } = true;
    public bool UseBingTextSearch { get; init; } = true;
    public bool UseDuckDuckGoTextSearch { get; init; }
    public bool UseCatalogPageExtraction { get; init; }
    public bool ValidateBingImageUrls { get; init; }
    public IReadOnlySet<string> AllowedCatalogDomains { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static EnrichmentOptions Parse(string[] args)
    {
        var parsed = ParseArgs(args);
        var allowedCatalogDomains = SplitValues(
                Get(parsed, "allowed-catalog-domain") ??
                Get(parsed, "allowed-catalog-domains") ??
                Environment.GetEnvironmentVariable("SPAREPARTS_IMAGE_ALLOWED_CATALOG_DOMAINS"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var realUpdate = GetBool(parsed, "real-update", defaultValue: false);

        return new EnrichmentOptions
        {
            DryRun = realUpdate ? false : GetBool(parsed, "dry-run", defaultValue: true),
            Yes = GetBool(parsed, "yes", defaultValue: false),
            Limit = GetInt(parsed, "limit"),
            VehicleId = GetInt(parsed, "vehicle-id"),
            PartId = GetInt(parsed, "part-id"),
            BatchSize = Math.Max(1, GetInt(parsed, "batch-size") ?? 50),
            Resume = GetBool(parsed, "resume", defaultValue: false),
            ReplaceExisting = GetBool(parsed, "replace-existing", defaultValue: true),
            ReprocessFailedOnly = GetBool(parsed, "reprocess-failed-only", defaultValue: false),
            ReprocessAll = GetBool(parsed, "reprocess-all", defaultValue: false),
            ReportOnly = GetBool(parsed, "report-only", defaultValue: false) ||
                         GetBool(parsed, "export-report-only", defaultValue: false),
            UpdateOem = GetBool(parsed, "update-oem", defaultValue: true),
            UpdateImages = GetBool(parsed, "update-images", defaultValue: true),
            DetectMissingParts = GetBool(parsed, "detect-missing-parts", defaultValue: true),
            AutoInsertMissingParts = GetBool(parsed, "auto-insert-missing-parts", defaultValue: false),
            HarvestPartsFromWeb = GetBool(parsed, "harvest-parts-from-web", defaultValue: false),
            SaveCandidates = GetBool(parsed, "save-candidates", defaultValue: true),
            MinConfidence = NormalizeConfidence(Get(parsed, "min-confidence") ?? "high"),
            ConnectionString = Get(parsed, "conn") ??
                               Get(parsed, "connection-string") ??
                               Environment.GetEnvironmentVariable("SPAREPARTS_DB_CONNECTION") ??
                               "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;",
            ApplyReviewCsv = Get(parsed, "apply-review-csv"),
            GoogleApiKey = Get(parsed, "google-api-key") ?? Environment.GetEnvironmentVariable("GOOGLE_CSE_API_KEY") ?? "",
            GoogleCx = Get(parsed, "google-cx") ?? Environment.GetEnvironmentVariable("GOOGLE_CSE_CX") ?? "",
            MaxQueriesPerPart = Math.Max(1, GetInt(parsed, "max-queries-per-part") ?? 6),
            MaxTextResultsPerQuery = Math.Max(1, GetInt(parsed, "max-text-results-per-query") ?? 8),
            CandidateLimitPerPart = Math.Max(1, GetInt(parsed, "candidate-limit-per-part") ?? 3),
            DelayMilliseconds = Math.Max(0, GetInt(parsed, "delay-ms") ?? 300),
            HttpTimeoutSeconds = Math.Max(5, GetInt(parsed, "http-timeout-seconds") ?? 20),
            AllowUnknownLicenseAutoUpdate = GetBool(parsed, "allow-unknown-license-auto-update", defaultValue: false),
            SearchReviewWithoutIdentifiers = GetBool(parsed, "search-review-without-identifiers", defaultValue: true),
            UseBingTextSearch = GetBool(parsed, "use-bing-text-search", defaultValue: true),
            UseDuckDuckGoTextSearch = GetBool(parsed, "use-duckduckgo-text-search", defaultValue: false),
            UseCatalogPageExtraction = GetBool(parsed, "use-catalog-page-extraction", defaultValue: false),
            ValidateBingImageUrls = GetBool(parsed, "validate-bing-image-urls", defaultValue: false),
            AllowedCatalogDomains = allowedCatalogDomains
        };
    }

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            var keyValue = arg[2..];
            var eq = keyValue.IndexOf('=');
            if (eq >= 0)
            {
                result[keyValue[..eq]] = keyValue[(eq + 1)..];
                continue;
            }

            var key = keyValue;
            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                result[key] = args[++i];
            }
            else
            {
                result[key] = "true";
            }
        }

        return result;
    }

    private static string? Get(IReadOnlyDictionary<string, string?> args, string key)
    {
        return args.TryGetValue(key, out var value) ? value : null;
    }

    private static bool GetBool(IReadOnlyDictionary<string, string?> args, string key, bool defaultValue)
    {
        if (!args.TryGetValue(key, out var value))
        {
            return defaultValue;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase);
    }

    private static int? GetInt(IReadOnlyDictionary<string, string?> args, string key)
    {
        return args.TryGetValue(key, out var value) &&
               int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string NormalizeConfidence(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "low" => "low",
            "medium" => "medium",
            _ => "high"
        };
    }

    private static IEnumerable<string> SplitValues(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var part in value.Split([',', ';', '|', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return part.ToLowerInvariant();
        }
    }
}
