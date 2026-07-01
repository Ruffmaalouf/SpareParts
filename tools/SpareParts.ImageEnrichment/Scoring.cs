using System.Globalization;

namespace SpareParts.ImageEnrichment;

internal enum ConfidenceLevel
{
    Low = 1,
    Medium = 2,
    High = 3
}

internal sealed record CandidateScore(
    ImageCandidate Candidate,
    SearchQuery Query,
    ImageValidation Validation,
    double Score,
    ConfidenceLevel Level,
    bool CanAutoApply,
    string Reason);

internal static class ConfidenceScorer
{
    public static CandidateScore Score(
        PartRecord part,
        SearchQuery query,
        ImageCandidate candidate,
        ImageValidation validation,
        IReadOnlyDictionary<string, string> selectedUrls,
        IReadOnlyList<PartRecord> allParts)
    {
        if (!validation.Reachable || !string.IsNullOrWhiteSpace(validation.SkipReason))
        {
            return new CandidateScore(candidate, query, validation, 0, ConfidenceLevel.Low, false, $"invalid: {validation.SkipReason}");
        }

        if (selectedUrls.TryGetValue(candidate.ImageUrl, out var existingKey) &&
            !existingKey.Equals(part.RelationshipKey, StringComparison.OrdinalIgnoreCase))
        {
            return new CandidateScore(candidate, query, validation, 0, ConfidenceLevel.Low, false, "duplicate candidate already selected for unrelated part");
        }

        var searchable = BuildSearchableText(candidate);
        var normalizedText = Program.NonAlphaNumericRegex().Replace(searchable, "");
        var identifiers = part.ExternalIdentifiers
            .Select(Program.NormalizeIdentifier)
            .Where(i => i.Length >= 4)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var identifierMatch = identifiers.Any(i => normalizedText.Contains(i, StringComparison.OrdinalIgnoreCase));

        var partTokens = CorePartTokens(part).ToList();
        var candidateTokens = Program.TokenRegex().Matches(searchable)
            .Select(m => m.Value.ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var tokenMatches = partTokens.Where(candidateTokens.Contains).ToList();
        var tokenOverlap = partTokens.Count == 0 ? 0 : (double)tokenMatches.Count / partTokens.Count;

        var vehicleMatches = 0;
        if (!string.IsNullOrWhiteSpace(part.CarMake) && searchable.Contains(part.CarMake, StringComparison.OrdinalIgnoreCase)) vehicleMatches++;
        if (!string.IsNullOrWhiteSpace(part.CarModel) && searchable.Contains(part.CarModel, StringComparison.OrdinalIgnoreCase)) vehicleMatches++;
        if (part.CarYear is int year && searchable.Contains(year.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)) vehicleMatches++;

        var score = 0d;
        var reasons = new List<string>();

        if (identifierMatch)
        {
            score += 52;
            reasons.Add("exact OEM/part-number match");
        }

        score += query.Tier switch
        {
            QueryTier.ExactIdentifier => 18,
            QueryTier.VehiclePart => 10,
            QueryTier.BrandPart => 6,
            _ => 0
        };
        reasons.Add($"query tier {query.Tier}");

        if (tokenOverlap > 0)
        {
            score += Math.Min(24, tokenOverlap * 24);
            reasons.Add($"part-token overlap {tokenMatches.Count}/{Math.Max(partTokens.Count, 1)}");
        }

        if (vehicleMatches > 0)
        {
            score += vehicleMatches * 5;
            reasons.Add($"vehicle tokens {vehicleMatches}/3");
        }

        if (!string.IsNullOrWhiteSpace(part.Brand) && searchable.Contains(part.Brand, StringComparison.OrdinalIgnoreCase))
        {
            score += 5;
            reasons.Add("brand match");
        }

        if (candidate.LicenseKnown)
        {
            score += 6;
            reasons.Add($"licensed source {candidate.Provider}");
        }

        if (validation.ContentLengthBytes is > 100_000)
        {
            score += 4;
            reasons.Add("large image");
        }

        score = Math.Clamp(score, 0, 100);

        var level = ConfidenceLevel.Low;
        if (identifierMatch && tokenOverlap >= 0.25 && score >= 78)
        {
            level = ConfidenceLevel.High;
        }
        else if ((identifierMatch && score >= 58) || (vehicleMatches >= 2 && tokenOverlap >= 0.35 && score >= 50))
        {
            level = ConfidenceLevel.Medium;
        }
        else if (score >= 45)
        {
            level = ConfidenceLevel.Medium;
        }

        // Upgrade CatalogPage or AutoZone results from trusted automotive domains to High confidence
        // when vehicle + part tokens match well, even without an OEM identifier match.
        // This allows real product images from AutoZone, RockAuto, O'Reilly, etc. to be auto-applied.
        if (!identifierMatch &&
            level == ConfidenceLevel.Medium &&
            candidate.UnknownLicenseCatalogSource &&
            candidate.Provider != "BingImages" &&
            candidate.Provider != "GoogleCSE" &&
            IsTrustedAutopartsDomain(candidate) &&
            vehicleMatches >= 2 &&
            tokenOverlap >= 0.25)
        {
            level = ConfidenceLevel.High;
            reasons.Add($"trusted autoparts catalog upgraded to High: {candidate.SourceDomain}");
        }

        var canAutoApply = level == ConfidenceLevel.High &&
                           (candidate.LicenseKnown || (!candidate.UnknownLicenseCatalogSource && candidate.Provider != "GoogleCSE"));

        if (level == ConfidenceLevel.High && candidate.UnknownLicenseCatalogSource && !IsTrustedAutopartsDomain(candidate))
        {
            canAutoApply = false;
            reasons.Add("unknown license catalog source requires review");
        }

        if (candidate.UnknownLicenseCatalogSource && candidate.Provider == "GoogleCSE")
        {
            reasons.Add("Google CSE candidate is review-only");
        }

        return new CandidateScore(
            candidate,
            query,
            validation,
            Math.Round(score, 2),
            level,
            canAutoApply,
            string.Join("; ", reasons));
    }

    private static readonly HashSet<string> TrustedAutopartsDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "autozone.com", "rockauto.com", "oreillyauto.com", "advanceautoparts.com",
        "napaonline.com", "pepboys.com", "partsgeek.com", "1aauto.com", "carid.com",
        "carparts.com", "fcautoparts.com", "oempartsonline.com", "maperformance.com",
        "contentassets.autozone.com", "oreillyauto.com", "autoparts.com"
    };

    private static bool IsTrustedAutopartsDomain(ImageCandidate candidate)
    {
        var checkTexts = new[] { candidate.SourceDomain, candidate.SourcePageUrl, candidate.ImageUrl };
        return checkTexts.Any(t =>
            !string.IsNullOrEmpty(t) &&
            TrustedAutopartsDomains.Any(d => t.Contains(d, StringComparison.OrdinalIgnoreCase)));
    }

    private static string BuildSearchableText(ImageCandidate candidate)
    {
        return string.Join(" ", new[]
        {
            candidate.Title,
            candidate.SourcePageUrl,
            candidate.ImageUrl,
            candidate.SourceDomain
        }.Where(s => !string.IsNullOrWhiteSpace(s))).ToLowerInvariant();
    }

    private static IEnumerable<string> CorePartTokens(PartRecord part)
    {
        var remove = new HashSet<string>(Program.StopWords, StringComparer.OrdinalIgnoreCase);
        foreach (var value in new[] { part.CarMake, part.CarModel, part.Brand, part.Category, part.CarYear?.ToString() })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var token in Program.TokenRegex().Matches(value.ToLowerInvariant()).Select(m => m.Value))
            {
                remove.Add(token);
            }
        }

        return Program.TokenRegex().Matches(part.Name.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length >= 3)
            .Where(t => !int.TryParse(t, out _))
            .Where(t => !remove.Contains(t))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}

