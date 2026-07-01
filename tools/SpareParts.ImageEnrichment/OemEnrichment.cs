using System.Globalization;
using System.Text.RegularExpressions;

namespace SpareParts.ImageEnrichment;

internal sealed partial class OemEnricher
{
    private static readonly HashSet<string> OemStopTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "OEM", "VIN", "BMW", "AUDI", "MERCEDES", "VOLVO", "TOYOTA", "HONDA", "HYUNDAI",
        "SPARE", "PART", "PARTS", "ENGINE", "FRONT", "REAR", "LEFT", "RIGHT", "USED",
        "GENUINE", "ORIGINAL", "NUMBER", "CATALOG", "IMAGE", "AUTO", "CAR"
    };

    private readonly ImageSearchClient _search;
    private readonly EnrichmentOptions _options;
    private readonly RunLogger _logger;

    public OemEnricher(ImageSearchClient search, EnrichmentOptions options, RunLogger logger)
    {
        _search = search;
        _options = options;
        _logger = logger;
    }

    public async Task<OemEnrichmentResult> EnrichAsync(PartRecord part)
    {
        var result = new OemEnrichmentResult
        {
            PartId = part.Id,
            InternalCode = part.InternalCode,
            PartName = part.Name,
            Vehicle = VehicleText(part),
            ExistingOemNumber = part.OemNumber
        };

        if (!string.IsNullOrWhiteSpace(part.OemNumber))
        {
            result.ProposedPrimaryOemNumber = part.OemNumber;
            result.ConfidenceScore = 100;
            result.ConfidenceLevel = "high";
            result.Status = "existing";
            result.MatchReason = "Part already has an OEM number; no lookup needed.";
            return result;
        }

        if (_options.ReportOnly)
        {
            result.Status = "report_only";
            result.ErrorMessage = "Report-only mode did not search OEM sources.";
            return result;
        }

        var best = default(OemCandidate);
        var alternatives = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var query in BuildQueries(part).Take(_options.MaxQueriesPerPart))
        {
            result.SearchQueriesExecuted++;
            IReadOnlyList<TextSearchResult> textResults;
            try
            {
                textResults = await _search.SearchTextAsync(query);
            }
            catch (Exception ex)
            {
                _logger.Warn($"OEM search failed for part {part.Id}, query '{query}': {ex.Message}");
                continue;
            }

            result.TextResultsFound += textResults.Count;
            foreach (var textResult in textResults)
            {
                foreach (var token in ExtractOemLikeTokens(textResult, part))
                {
                    var scored = ScoreCandidate(part, query, textResult, token);
                    if (!alternatives.TryGetValue(scored.Number, out var existingScore) || scored.Score > existingScore)
                    {
                        alternatives[scored.Number] = scored.Score;
                    }

                    if (best is null || scored.Score > best.Score)
                    {
                        best = scored;
                    }
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(_options.DelayMilliseconds));
        }

        if (best is null)
        {
            result.Status = "failed";
            result.ErrorMessage = result.TextResultsFound == 0
                ? "No text search results returned from configured OEM sources."
                : "Text search ran, but no OEM-like numbers were found in titles/snippets.";
            result.MatchReason = $"Queries executed: {result.SearchQueriesExecuted}; text results: {result.TextResultsFound}.";
            return result;
        }

        result.ProposedPrimaryOemNumber = best.Number;
        result.AlternativeOemNumbers = string.Join("; ", alternatives
            .Where(kvp => !kvp.Key.Equals(best.Number, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kvp => kvp.Value)
            .Take(6)
            .Select(kvp => kvp.Key));
        result.SourceUrl = best.Source.Url;
        result.SourceDomain = Program.DomainFromUrl(best.Source.Url);
        result.SearchQueryUsed = best.Query;
        result.ConfidenceScore = best.Score;
        result.ConfidenceLevel = best.Level;
        result.MatchReason = best.Reason;
        result.Status = best.Level == "high" ? "verified" : best.Level == "medium" ? "pending_review" : "rejected";
        if (best.Level == "low")
        {
            result.ErrorMessage = "Best OEM-like number was low confidence and was not proposed for update.";
        }

        return result;
    }

    private static IEnumerable<string> BuildQueries(PartRecord part)
    {
        var name = Clean(part.Name);
        var make = Clean(part.CarMake);
        var model = Clean(part.CarModel);
        var year = part.CarYear?.ToString(CultureInfo.InvariantCulture);
        var engine = Clean(part.EngineType);
        var category = Clean(part.Category);

        if (!string.IsNullOrWhiteSpace(part.ChassisNumber) && part.ChassisNumber.Length >= 8)
        {
            yield return $"{part.ChassisNumber} {name} OEM number";
        }

        yield return $"{make} {model} {year} {engine} {name} OEM part number";
        yield return $"{make} {model} {year} {name} genuine OEM";
        yield return $"{make} {model} {year} {category} OEM number";
        yield return $"{name} OEM number {make} {model}";
    }

    private static OemCandidate ScoreCandidate(PartRecord part, string query, TextSearchResult source, string number)
    {
        var searchable = $"{source.Title} {source.Snippet} {source.Url}";
        var lower = searchable.ToLowerInvariant();
        var score = 22d;
        var reasons = new List<string> { "OEM-like token found in search result" };

        var vehicleMatches = 0;
        if (!string.IsNullOrWhiteSpace(part.CarMake) && lower.Contains(part.CarMake.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)) vehicleMatches++;
        if (!string.IsNullOrWhiteSpace(part.CarModel) && lower.Contains(part.CarModel.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)) vehicleMatches++;
        if (part.CarYear is int year && lower.Contains(year.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)) vehicleMatches++;
        if (!string.IsNullOrWhiteSpace(part.EngineType) && lower.Contains(part.EngineType.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)) vehicleMatches++;
        score += vehicleMatches * 10;
        reasons.Add($"vehicle tokens {vehicleMatches}/4");

        var partTokens = CorePartTokens(part).ToList();
        var tokenMatches = partTokens.Count(t => lower.Contains(t, StringComparison.OrdinalIgnoreCase));
        score += Math.Min(24, tokenMatches * 8);
        reasons.Add($"part-token matches {tokenMatches}/{Math.Max(partTokens.Count, 1)}");

        if (lower.Contains("oem", StringComparison.OrdinalIgnoreCase))
        {
            score += 12;
            reasons.Add("result mentions OEM");
        }

        if (lower.Contains("part number", StringComparison.OrdinalIgnoreCase) ||
            lower.Contains("part no", StringComparison.OrdinalIgnoreCase))
        {
            score += 8;
            reasons.Add("result mentions part number");
        }

        var domain = Program.DomainFromUrl(source.Url);
        var trustedCatalog = KnownCatalogDomain(domain) && !source.Provider.Equals("Bing", StringComparison.OrdinalIgnoreCase);
        if (trustedCatalog)
        {
            score += 8;
            reasons.Add($"catalog-like source {domain}");
        }

        score = Math.Clamp(Math.Round(score, 2), 0, 100);
        var level = score >= 88 && vehicleMatches >= 2 && tokenMatches >= 1
            ? "high"
            : score >= 62 && (vehicleMatches >= 2 || tokenMatches >= 1)
                ? "medium"
                : "low";

        if (level == "high" && !trustedCatalog)
        {
            level = "medium";
            reasons.Add("broad text result requires review before OEM update");
        }

        return new OemCandidate(number, source, query, score, level, string.Join("; ", reasons));
    }

    private static IEnumerable<string> ExtractOemLikeTokens(TextSearchResult result, PartRecord part)
    {
        var text = $"{result.Title} {result.Snippet}";
        foreach (Match match in OemTokenRegex().Matches(text.ToUpperInvariant()))
        {
            var token = match.Value.Trim(' ', '.', ',', ':', ';', '(', ')', '[', ']');
            token = Regex.Replace(token, @"\s+", "", RegexOptions.CultureInvariant);
            if (!LooksLikePartNumber(token, part))
            {
                continue;
            }

            yield return token;
        }
    }

    private static bool LooksLikePartNumber(string token, PartRecord part)
    {
        if (token.Length < 5 || token.Length > 24)
        {
            return false;
        }

        if (!token.Any(char.IsDigit))
        {
            return false;
        }

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric) &&
            numeric is >= 1900 and <= 2099)
        {
            return false;
        }

        if (OemStopTokens.Any(stop => token.Contains(stop, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (Regex.IsMatch(token, "[A-Z]{6,}", RegexOptions.CultureInvariant))
        {
            return false;
        }

        var normalized = Program.NormalizeIdentifier(token);
        var rejectPieces = new[] { part.CarMake, part.CarModel, part.Name, part.Category, part.EngineType }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .SelectMany(s => Program.TokenRegex().Matches(s!.ToUpperInvariant()).Select(m => m.Value))
            .Where(s => s.Length >= 5);

        return !rejectPieces.Any(piece =>
        {
            var normalizedPiece = Program.NormalizeIdentifier(piece);
            return normalizedPiece.Length >= 3 &&
                   normalized.Contains(normalizedPiece, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static IEnumerable<string> CorePartTokens(PartRecord part)
    {
        return Program.TokenRegex().Matches(part.Name.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length >= 3)
            .Where(t => !int.TryParse(t, out _))
            .Where(t => !Program.StopWords.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string VehicleText(PartRecord part)
    {
        return string.Join(" ", new[] { part.CarYear?.ToString(CultureInfo.InvariantCulture), part.CarMake, part.CarModel, part.EngineType }
            .Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string Clean(string? value) => Regex.Replace(value ?? "", @"\s+", " ").Trim();

    private static bool KnownCatalogDomain(string domain)
    {
        return domain.Contains("realoem", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("partslink", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("catcar", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("nemigaparts", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("oemcats", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("7zap", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("amayama", StringComparison.OrdinalIgnoreCase) ||
               domain.Contains("partsouq", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\b[A-Z0-9]{2,}[-\s]?[A-Z0-9]{3,}(?:[-\s]?[A-Z0-9]{2,}){0,3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex OemTokenRegex();

    private sealed record OemCandidate(
        string Number,
        TextSearchResult Source,
        string Query,
        double Score,
        string Level,
        string Reason);
}
