using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SpareParts.ImageEnrichment;

internal static partial class Program
{
    private const int MinImageBytes = 5_000;
    private static readonly string[] AllowedImageContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    internal static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "with", "for", "set", "pair", "assembly", "auto", "part", "parts",
        "spare", "used", "genuine", "after", "aftermarket", "oem", "new", "front", "rear"
    };

    private static readonly HashSet<string> PlaceholderNeedles = new(StringComparer.OrdinalIgnoreCase)
    {
        "placeholder", "no-image", "noimage", "image-not-available", "not-available",
        "coming-soon", "default-image", "missing-image"
    };

    private static readonly HashSet<string> WatermarkNeedles = new(StringComparer.OrdinalIgnoreCase)
    {
        "shutterstock", "gettyimages", "istockphoto", "dreamstime", "depositphotos", "alamy"
    };

    public static async Task<int> Main(string[] args)
    {
        var options = EnrichmentOptions.Parse(args);
        var startedAt = DateTimeOffset.Now;
        var stamp = startedAt.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var root = FindRepositoryRoot();
        var reportsDir = root / "reports";
        var backupsDir = root / "backups";
        var logsDir = root / "logs";
        Directory.CreateDirectory(reportsDir.FullName);
        Directory.CreateDirectory(backupsDir.FullName);
        Directory.CreateDirectory(logsDir.FullName);

        using var logger = new RunLogger(logsDir / $"part-image-enrichment-{stamp}.log");
        logger.Info("SpareParts part image enrichment");
        logger.Info($"Started: {startedAt:O}");
        logger.Info($"Mode: {(options.DryRun ? "dry-run - no database writes" : "live - database writes enabled")}");
        logger.Info($"Limit: {(options.Limit is null ? "all" : options.Limit.Value.ToString(CultureInfo.InvariantCulture))}");
        logger.Info($"Vehicle filter: {(options.VehicleId is null ? "none" : options.VehicleId.Value.ToString(CultureInfo.InvariantCulture))}");
        logger.Info($"Part filter: {(options.PartId is null ? "none" : options.PartId.Value.ToString(CultureInfo.InvariantCulture))}");
        logger.Info($"Batch size: {options.BatchSize}");
        logger.Info($"Resume: {options.Resume}");
        logger.Info($"Replace existing: {options.ReplaceExisting}");
        logger.Info($"Report only: {options.ReportOnly}");
        logger.Info($"Update OEM: {options.UpdateOem}");
        logger.Info($"Update images: {options.UpdateImages}");
        logger.Info($"Detect missing parts: {options.DetectMissingParts}");
        logger.Info($"Save candidates: {options.SaveCandidates}");
        logger.Info($"Minimum confidence: {options.MinConfidence}");
        logger.Info($"Unknown-license auto updates: {options.AllowUnknownLicenseAutoUpdate}");
        logger.Info($"Search review without identifiers: {options.SearchReviewWithoutIdentifiers}");
        logger.Info($"Bing text/catalog fallback: {options.UseBingTextSearch}");
        logger.Info($"DuckDuckGo fallback: {options.UseDuckDuckGoTextSearch}");
        logger.Info($"Catalog page extraction: {options.UseCatalogPageExtraction}");
        logger.Info($"Validate Bing image URLs: {options.ValidateBingImageUrls}");

        await using var conn = new SqlConnection(options.ConnectionString);
        await conn.OpenAsync();
        logger.Info("Connected to database.");

        var schema = await DatabaseSchema.LoadAsync(conn);
        var schemaReport = reportsDir / $"part_image_schema_{stamp}.txt";
        await File.WriteAllTextAsync(schemaReport.FullName, schema.ToReportText());
        logger.Info($"Schema report: {schemaReport}");

        var backup = await BackupExporter.ExportAsync(conn, backupsDir, reportsDir, stamp, schema, logger);
        logger.Info($"Parts image backup: {backup.PartsCsv}");
        logger.Info($"Rollback SQL: {backup.RollbackSql}");

        if (!options.DryRun)
        {
            if (!options.Yes)
            {
                logger.Error("Live mode requires --yes. Aborting before any database write.");
                return 2;
            }

            if (!File.Exists(backup.RollbackSql.FullName))
            {
                logger.Error("Rollback SQL was not created. Aborting before any database write.");
                return 3;
            }

            await EnsureEnrichmentTablesAsync(conn);
            schema = await DatabaseSchema.LoadAsync(conn);
            logger.Info("Enrichment tables are ready.");
        }

        if (!string.IsNullOrWhiteSpace(options.ApplyReviewCsv))
        {
            if (options.DryRun)
            {
                logger.Error("--apply-review-csv requires --dry-run false.");
                return 4;
            }

            var appliedFromReview = await ApplyApprovedReviewCsvAsync(conn, options.ApplyReviewCsv, logger);
            logger.Info($"Applied {appliedFromReview} admin-approved review rows.");
            return 0;
        }

        var allParts = await PartRepository.FetchPartsAsync(conn);
        var allVehicles = await PartRepository.FetchVehiclesAsync(conn);
        var totalActiveParts = allParts.Count;
        logger.Info($"Active parts found: {totalActiveParts}");
        logger.Info($"Donor vehicles found: {allVehicles.Count}");

        var urlCounts = allParts
            .Where(p => !string.IsNullOrWhiteSpace(p.CurrentImageUrl))
            .GroupBy(p => p.CurrentImageUrl!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var failedOnlyIds = options.ReprocessFailedOnly
            ? await PartRepository.FetchFailedPartIdsAsync(conn, schema)
            : new HashSet<int>();

        var progressPath = logsDir / "part-image-enrichment-progress.json";
        var processedFromProgress = options.Resume && progressPath.Exists
            ? ProgressState.Load(progressPath).ProcessedPartIds
            : new HashSet<int>();

        var processedFromDb = options.Resume && !options.ReprocessAll
            ? await PartRepository.FetchProcessedPartIdsAsync(conn, schema)
            : new HashSet<int>();

        var parts = allParts
            .Where(p => options.PartId is null || p.Id == options.PartId.Value)
            .Where(p => options.VehicleId is null || p.UsedCarId == options.VehicleId.Value)
            .Where(p => !options.ReprocessFailedOnly || failedOnlyIds.Contains(p.Id))
            .Where(p => options.ReprocessAll || !processedFromProgress.Contains(p.Id))
            .Where(p => options.ReprocessAll || !processedFromDb.Contains(p.Id))
            .OrderBy(p => p.Id)
            .ToList();

        if (options.Limit is int limit)
        {
            parts = parts.Take(limit).ToList();
        }

        logger.Info($"Parts selected for this run: {parts.Count}");
        if (parts.Count == 0 && !options.DetectMissingParts && !options.HarvestPartsFromWeb)
        {
            logger.Info("Nothing to process.");
            return 0;
        }

        using var http = BuildHttpClient(options);
        var search = new ImageSearchClient(http, options, logger);
        var oemEnricher = new OemEnricher(search, options, logger);
        var validationCache = new Dictionary<string, ImageValidation>(StringComparer.OrdinalIgnoreCase);
        var selectedUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<EnrichmentResult>();
        var imageCandidates = new List<ImageCandidateAudit>();
        var oemResults = new List<OemEnrichmentResult>();
        IReadOnlyList<MissingPartCandidate> missingCandidates = [];
        var vehiclesScannedCount = 0;
        var progress = new ProgressState();
        foreach (var id in processedFromProgress)
        {
            progress.ProcessedPartIds.Add(id);
        }

        // Skip the enrichment loop entirely when neither images nor OEM are being updated
        if (!options.UpdateImages && !options.UpdateOem)
        {
            logger.Info("Both --update-images and --update-oem are false — skipping part enrichment loop.");
            parts = [];
        }

        for (var batchStart = 0; batchStart < parts.Count; batchStart += options.BatchSize)
        {
            var batch = parts.Skip(batchStart).Take(options.BatchSize).ToList();
            logger.Info($"Batch {(batchStart / options.BatchSize) + 1}: part IDs {batch.First().Id}..{batch.Last().Id}");
            var batchResults = new List<EnrichmentResult>();
            var batchImageCandidates = new List<ImageCandidateAudit>();
            var batchOemResults = new List<OemEnrichmentResult>();

            foreach (var part in batch)
            {
                logger.Info($"Processing {part.Id}: {part.Name}");
                var effectivePart = part;
                if (options.UpdateOem)
                {
                    var oemResult = await oemEnricher.EnrichAsync(part);
                    oemResults.Add(oemResult);
                    batchOemResults.Add(oemResult);
                    if (!string.IsNullOrWhiteSpace(oemResult.ProposedPrimaryOemNumber) &&
                        oemResult.ConfidenceLevel is "high" or "medium")
                    {
                        effectivePart = part with { OemNumber = oemResult.ProposedPrimaryOemNumber };
                    }
                    logger.Info($"OEM {part.Id}: {oemResult.Status}, confidence={oemResult.ConfidenceLevel} {oemResult.ConfidenceScore:0.0}");
                }

                if (options.UpdateImages)
                {
                    var result = await ProcessPartAsync(effectivePart, allParts, urlCounts, http, search, validationCache, selectedUrls, options, logger, batchImageCandidates);
                    results.Add(result);
                    batchResults.Add(result);
                    logger.Info($"Image {part.Id}: {result.Status}, current={result.CurrentImageStatus}, confidence={result.ConfidenceLevel} {result.ConfidenceScore:0.0}, candidates={result.CandidateImagesFound}, rejected={result.CandidateImagesRejected}");
                }

                progress.ProcessedPartIds.Add(part.Id);
                progress.LastProcessedPartId = part.Id;
                progress.LastRunAtUtc = DateTimeOffset.UtcNow;
                await progress.SaveAsync(progressPath);
            }

            imageCandidates.AddRange(batchImageCandidates);

            if (!options.DryRun)
            {
                await WriteLiveBatchAsync(conn, batchResults, batchOemResults, batchImageCandidates, options, logger);
            }
        }

        if (options.DetectMissingParts)
        {
            var vehiclesForDetection = allVehicles
                .Where(v => options.VehicleId is null || v.Id == options.VehicleId.Value)
                .ToList();
            vehiclesScannedCount = vehiclesForDetection.Count;
            missingCandidates = MissingPartsDetector.Detect(vehiclesForDetection, allParts, options.VehicleId);
            logger.Info($"Missing expected part candidates: {missingCandidates.Count}");
            if (!options.DryRun)
            {
                await WriteMissingCandidateBatchAsync(conn, missingCandidates, logger);
            }

            if (options.AutoInsertMissingParts && missingCandidates.Count > 0)
            {
                logger.Info("Auto-inserting missing parts into dbo.Parts...");
                var inserter = new MissingPartsInserter(conn, options, logger, oemEnricher);
                await inserter.LoadAsync();
                var insertedParts = await inserter.InsertAsync(missingCandidates);
                var inserted = insertedParts.Count(r => r.Inserted);
                var withOem = insertedParts.Count(r => r.OemFound);
                var skipped = insertedParts.Count(r => !r.Inserted);
                logger.Info($"Auto-insert complete: {inserted} parts created, {withOem} with OEM numbers, {skipped} skipped.");
            }
        }

        if (options.HarvestPartsFromWeb)
        {
            logger.Info("Web catalog harvest: searching internet for vehicle parts with OEM numbers...");
            var harvesterVehicles = allVehicles
                .Where(v => options.VehicleId is null || v.Id == options.VehicleId.Value)
                .ToList();
            var harvester = new WebCatalogHarvester(search, conn, options, logger);
            await harvester.LoadAsync();
            var harvestSummary = await harvester.HarvestAllVehiclesAsync(harvesterVehicles);
            logger.Info($"Web harvest complete: {harvestSummary.VehiclesProcessed} vehicles, " +
                        $"{harvestSummary.PartsDiscovered} parts discovered, " +
                        $"{harvestSummary.PartsInserted} inserted, " +
                        $"{harvestSummary.PartsSkipped} already existed.");
        }

        var report = await ReportWriter.WriteAsync(reportsDir, stamp, options, totalActiveParts, parts.Count, vehiclesScannedCount, results, imageCandidates, oemResults, missingCandidates, backup, schemaReport);
        logger.Info($"CSV report: {report.ResultsCsv}");
        logger.Info($"Image candidates CSV: {report.ImageCandidatesCsv}");
        logger.Info($"OEM CSV: {report.OemCsv}");
        logger.Info($"Missing expected parts CSV: {report.MissingPartsCsv}");
        logger.Info($"Review CSV: {report.ReviewCsv}");
        logger.Info($"HTML report: {report.HtmlReport}");
        logger.Info($"Summary JSON: {report.SummaryJson}");

        var summary = RunSummary.FromResults(totalActiveParts, parts.Count, vehiclesScannedCount, results, imageCandidates, oemResults, missingCandidates, options, backup, report);
        logger.Info("Summary:");
        foreach (var line in summary.ToLines())
        {
            logger.Info(line);
        }

        return 0;
    }

    private static HttpClient BuildHttpClient(EnrichmentOptions options)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SpareParts-ImageEnricher/2.0 (+local admin review)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        return client;
    }

    private static async Task<EnrichmentResult> ProcessPartAsync(
        PartRecord part,
        IReadOnlyList<PartRecord> allParts,
        IReadOnlyDictionary<string, int> currentUrlCounts,
        HttpClient http,
        ImageSearchClient search,
        Dictionary<string, ImageValidation> validationCache,
        Dictionary<string, string> selectedUrls,
        EnrichmentOptions options,
        RunLogger logger,
        List<ImageCandidateAudit> imageCandidateAudits)
    {
        var currentStatus = await AuditCurrentImageAsync(part, currentUrlCounts, http, validationCache);
        var result = EnrichmentResult.FromPart(part, currentStatus);

        if (options.ReportOnly)
        {
            result.Status = "report_only";
            result.ErrorMessage = "Report-only mode did not search for replacements.";
            return result;
        }

        if (!options.ReplaceExisting && currentStatus.Status == "trusted")
        {
            result.Status = "skipped";
            result.ErrorMessage = "Existing image is trusted and --replace-existing is false.";
            return result;
        }

        if (!part.ExternalIdentifiers.Any() && !options.SearchReviewWithoutIdentifiers)
        {
            result.Status = "skipped";
            result.ErrorMessage = "No OEM or external part number is available; broad visual search is review-only and disabled by default.";
            return result;
        }

        CandidateScore? best = null;
        var scoredCandidates = new List<CandidateScore>();
        var rejectionReasons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var queries = QueryBuilder.Build(part);
        foreach (var query in queries.Take(options.MaxQueriesPerPart))
        {
            result.SearchQueriesExecuted++;
            IReadOnlyList<ImageCandidate> candidates;
            try
            {
                candidates = await search.SearchAsync(query.Text);
            }
            catch (Exception ex)
            {
                logger.Warn($"Search failed for part {part.Id}, query '{query.Text}': {ex.Message}");
                AddReason(rejectionReasons, "search_error");
                continue;
            }

            result.CandidateImagesFound += candidates.Count;
            foreach (var candidate in candidates)
            {
                var validation = candidate.Provider == "BingImages" && !options.ValidateBingImageUrls
                    ? new ImageValidation(true, "image/search-result", null, null)
                    : await ValidateImageAsync(
                        candidate.Provider == "BingImages" && !string.IsNullOrWhiteSpace(candidate.ThumbUrl)
                            ? candidate.ThumbUrl!
                            : candidate.ImageUrl,
                        http,
                        validationCache);
                var score = ConfidenceScorer.Score(part, query, candidate, validation, selectedUrls, allParts);
                scoredCandidates.Add(score);
                if (IsRejectedCandidate(score))
                {
                    AddReason(rejectionReasons, RejectionReason(score));
                }

                if (best is null || score.Score > best.Score)
                {
                    best = score;
                }
            }

            if (best is not null && best.Level == ConfidenceLevel.High && best.CanAutoApply)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(options.DelayMilliseconds));
        }

        result.CandidateImagesRejected = scoredCandidates.Count(IsRejectedCandidate);
        result.RejectionReasons = string.Join("; ", rejectionReasons.OrderByDescending(kvp => kvp.Value).Select(kvp => $"{kvp.Key}={kvp.Value}"));
        result.TopCandidateUrls = string.Join(" | ", scoredCandidates
            .OrderByDescending(s => s.Score)
            .Take(options.CandidateLimitPerPart)
            .Select(s => s.Candidate.ImageUrl));

        var rank = 1;
        foreach (var score in scoredCandidates.OrderByDescending(s => s.Score).Take(options.CandidateLimitPerPart))
        {
            imageCandidateAudits.Add(BuildImageCandidateAudit(part, score, rank++));
        }

        if (best is null)
        {
            result.Status = "skipped";
            result.ErrorMessage = result.SearchQueriesExecuted == 0
                ? "No image search query was built for this part."
                : "No candidate image was returned by configured sources.";
            return result;
        }

        if (best.Level == ConfidenceLevel.Low)
        {
            result.Status = "skipped";
            result.ErrorMessage = "Best candidate was low confidence.";
            return result;
        }

        result.SelectedImageUrl = best.Candidate.ImageUrl;
        result.ThumbUrl = best.Candidate.ThumbUrl;
        result.SourcePageUrl = best.Candidate.SourcePageUrl;
        result.SourceDomain = best.Candidate.SourceDomain;
        result.SearchQueryUsed = best.Query.Text;
        result.ConfidenceScore = best.Score;
        result.ConfidenceLevel = best.Level.ToString().ToLowerInvariant();
        result.MatchReason = best.Reason;
        result.ContentType = best.Validation.ContentType;
        result.ContentLengthBytes = best.Validation.ContentLengthBytes;
        result.ImageReachable = best.Validation.Reachable;
        result.Provider = best.Candidate.Provider;
        result.License = best.Candidate.License;
        result.SourceCanAutoApply = best.CanAutoApply;

        var operatorAllowedUnknownCatalog = best.Level == ConfidenceLevel.High &&
                                            options.AllowUnknownLicenseAutoUpdate &&
                                            best.Candidate.UnknownLicenseCatalogSource &&
                                            best.Candidate.Provider != "BingImages";

        if (best.Level == ConfidenceLevel.High && (best.CanAutoApply || operatorAllowedUnknownCatalog))
        {
            result.Status = "auto_approved";
            selectedUrls[best.Candidate.ImageUrl] = part.RelationshipKey;
            return result;
        }

        result.Status = "pending_review";
        result.ErrorMessage = best.CanAutoApply
            ? "Medium confidence requires admin review."
            : "High or medium match has unknown catalog/license terms and requires admin review.";
        return result;
    }

    private static ImageCandidateAudit BuildImageCandidateAudit(PartRecord part, CandidateScore score, int rank)
    {
        var status = score.Level == ConfidenceLevel.High && score.CanAutoApply
            ? "auto_approved_candidate"
            : score.Level == ConfidenceLevel.Medium
                ? "pending_review_candidate"
                : "rejected_candidate";

        return new ImageCandidateAudit
        {
            PartId = part.Id,
            InternalCode = part.InternalCode,
            PartName = part.Name,
            Vehicle = string.Join(" ", new[] { part.CarYear?.ToString(CultureInfo.InvariantCulture), part.CarMake, part.CarModel, part.EngineType }
                .Where(v => !string.IsNullOrWhiteSpace(v))),
            CandidateRank = rank,
            SearchQueryUsed = score.Query.Text,
            QueryTier = score.Query.Tier.ToString(),
            ImageUrl = score.Candidate.ImageUrl,
            ThumbUrl = score.Candidate.ThumbUrl,
            SourcePageUrl = score.Candidate.SourcePageUrl,
            SourceDomain = score.Candidate.SourceDomain,
            Provider = score.Candidate.Provider,
            License = score.Candidate.License,
            LicenseKnown = score.Candidate.LicenseKnown,
            ConfidenceScore = score.Score,
            ConfidenceLevel = score.Level.ToString().ToLowerInvariant(),
            MatchReason = score.Reason,
            ImageReachable = score.Validation.Reachable,
            ContentType = score.Validation.ContentType,
            ContentLengthBytes = score.Validation.ContentLengthBytes,
            RejectionReason = IsRejectedCandidate(score) ? RejectionReason(score) : null,
            Status = status
        };
    }

    private static bool IsRejectedCandidate(CandidateScore score)
    {
        return !score.Validation.Reachable ||
               !string.IsNullOrWhiteSpace(score.Validation.SkipReason) ||
               score.Level == ConfidenceLevel.Low;
    }

    private static string RejectionReason(CandidateScore score)
    {
        if (!score.Validation.Reachable)
        {
            return "unreachable_image";
        }

        if (!string.IsNullOrWhiteSpace(score.Validation.SkipReason))
        {
            return score.Validation.SkipReason!;
        }

        if (score.Level == ConfidenceLevel.Low)
        {
            return "low_confidence";
        }

        if (!score.CanAutoApply)
        {
            return "review_required_or_unknown_license";
        }

        return "not_rejected";
    }

    private static void AddReason(Dictionary<string, int> reasons, string reason)
    {
        reasons[reason] = reasons.TryGetValue(reason, out var count) ? count + 1 : 1;
    }

    private static bool MeetsMinimum(ConfidenceLevel level, string minimum)
    {
        var min = minimum.Equals("medium", StringComparison.OrdinalIgnoreCase)
            ? ConfidenceLevel.Medium
            : minimum.Equals("low", StringComparison.OrdinalIgnoreCase)
                ? ConfidenceLevel.Low
                : ConfidenceLevel.High;

        return (int)level >= (int)min;
    }

    private static async Task<CurrentImageAudit> AuditCurrentImageAsync(
        PartRecord part,
        IReadOnlyDictionary<string, int> currentUrlCounts,
        HttpClient http,
        Dictionary<string, ImageValidation> validationCache)
    {
        var url = part.CurrentImageUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return new CurrentImageAudit("missing", "No current image URL.", false, null, null);
        }

        var lowered = url.ToLowerInvariant();
        if (PlaceholderNeedles.Any(lowered.Contains))
        {
            return new CurrentImageAudit("placeholder", "URL looks like a placeholder.", false, null, null);
        }

        if (currentUrlCounts.TryGetValue(url, out var count) && count > 1)
        {
            return new CurrentImageAudit("duplicated", $"Same image URL is used by {count} active parts.", false, null, null);
        }

        var validation = await ValidateImageAsync(url, http, validationCache);
        if (!validation.Reachable)
        {
            return new CurrentImageAudit("broken", validation.SkipReason ?? "Current image URL is not reachable.", false, validation.ContentType, validation.ContentLengthBytes);
        }

        if (!string.IsNullOrWhiteSpace(validation.SkipReason))
        {
            return new CurrentImageAudit("suspicious", validation.SkipReason, validation.Reachable, validation.ContentType, validation.ContentLengthBytes);
        }

        if (lowered.Contains("api.openverse.org/v1/images/") || lowered.Contains("/thumb/"))
        {
            return new CurrentImageAudit("wrong", "Openverse thumbnail URL from previous broad import; no part-level proof.", validation.Reachable, validation.ContentType, validation.ContentLengthBytes);
        }

        var normalizedUrl = NormalizeIdentifier(url);
        var identifiers = part.ExternalIdentifiers.Select(NormalizeIdentifier).Where(s => s.Length >= 4);
        if (identifiers.Any(normalizedUrl.Contains))
        {
            return new CurrentImageAudit("trusted", "Existing URL contains an exact OEM or external part number.", validation.Reachable, validation.ContentType, validation.ContentLengthBytes);
        }

        return new CurrentImageAudit("suspicious", "Reachable, but no strong proof that it matches this part.", validation.Reachable, validation.ContentType, validation.ContentLengthBytes);
    }

    private static async Task<ImageValidation> ValidateImageAsync(string url, HttpClient http, Dictionary<string, ImageValidation> cache)
    {
        if (cache.TryGetValue(url, out var cached))
        {
            return cached;
        }

        var result = await ValidateImageUncachedAsync(url, http);
        cache[url] = result;
        return result;
    }

    private static async Task<ImageValidation> ValidateImageUncachedAsync(string url, HttpClient http)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return new ImageValidation(false, null, null, "invalid_url");
        }

        if (WatermarkNeedles.Any(n => uri.Host.Contains(n, StringComparison.OrdinalIgnoreCase)))
        {
            return new ImageValidation(true, null, null, "watermarked_source");
        }

        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, uri);
            using var headResp = await http.SendAsync(head, HttpCompletionOption.ResponseHeadersRead);
            if (headResp.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.Forbidden)
            {
                return await ValidateWithGetAsync(uri, http);
            }

            return ValidateHeaders(headResp);
        }
        catch
        {
            try
            {
                return await ValidateWithGetAsync(uri, http);
            }
            catch (Exception ex)
            {
                return new ImageValidation(false, null, null, $"request_error: {ex.Message}");
            }
        }
    }

    private static async Task<ImageValidation> ValidateWithGetAsync(Uri uri, HttpClient http)
    {
        using var get = new HttpRequestMessage(HttpMethod.Get, uri);
        get.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 4095);
        using var resp = await http.SendAsync(get, HttpCompletionOption.ResponseHeadersRead);
        return ValidateHeaders(resp);
    }

    private static ImageValidation ValidateHeaders(HttpResponseMessage resp)
    {
        var contentType = resp.Content.Headers.ContentType?.MediaType;
        var contentLength = resp.Content.Headers.ContentLength;
        if (!resp.IsSuccessStatusCode)
        {
            return new ImageValidation(false, contentType, contentLength, $"http_{(int)resp.StatusCode}");
        }

        if (contentType is null || !AllowedImageContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return new ImageValidation(true, contentType, contentLength, "not_image_content_type");
        }

        if (contentLength is not null && contentLength < MinImageBytes)
        {
            return new ImageValidation(true, contentType, contentLength, "too_small");
        }

        return new ImageValidation(true, contentType, contentLength, null);
    }

    private static async Task WriteLiveBatchAsync(
        SqlConnection conn,
        IReadOnlyList<EnrichmentResult> imageResults,
        IReadOnlyList<OemEnrichmentResult> oemResults,
        IReadOnlyList<ImageCandidateAudit> imageCandidates,
        EnrichmentOptions options,
        RunLogger logger)
    {
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            foreach (var result in imageResults)
            {
                await UpsertEnrichmentRowAsync(conn, tx, result);
            }

            if (options.SaveCandidates && imageCandidates.Count > 0)
            {
                await ReplaceImageCandidateRowsAsync(conn, tx, imageCandidates);
            }

            foreach (var result in oemResults)
            {
                await UpsertOemEnrichmentRowAsync(conn, tx, result);
            }

            var applied = 0;
            foreach (var result in imageResults.Where(r => r.Status == "auto_approved" && !string.IsNullOrWhiteSpace(r.SelectedImageUrl)))
            {
                await ApplyImageToPartAsync(conn, tx, result.PartId, result.SelectedImageUrl!);
                applied++;
            }

            var oemApplied = 0;
            foreach (var result in oemResults.Where(r => r.Status == "verified" &&
                                                        string.IsNullOrWhiteSpace(r.ExistingOemNumber) &&
                                                        !string.IsNullOrWhiteSpace(r.ProposedPrimaryOemNumber)))
            {
                await ApplyOemToPartAsync(conn, tx, result.PartId, result.ProposedPrimaryOemNumber!);
                oemApplied++;
            }

            await tx.CommitAsync();
            logger.Info($"Live batch committed. Image rows: {imageResults.Count}; image candidates: {imageCandidates.Count}; OEM rows: {oemResults.Count}; image URLs applied: {applied}; OEMs applied: {oemApplied}.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.Error($"Live batch rolled back: {ex.Message}");
        }
    }

    private static async Task EnsureEnrichmentTablesAsync(SqlConnection conn)
    {
        const string sql = """
IF OBJECT_ID('dbo.PartImageEnrichment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartImageEnrichment
    (
        Id                  INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartImageEnrichment PRIMARY KEY,
        PartId              INT            NOT NULL,
        SelectedImageUrl    NVARCHAR(1000) NULL,
        ThumbUrl            NVARCHAR(1000) NULL,
        SourcePageUrl       NVARCHAR(1000) NULL,
        SourceDomain        NVARCHAR(200)  NULL,
        SearchQueryUsed     NVARCHAR(500)  NULL,
        ConfidenceScore     DECIMAL(5,2)   NOT NULL CONSTRAINT DF_PIE_ConfidenceScore DEFAULT 0,
        ConfidenceLevel     NVARCHAR(20)   NOT NULL CONSTRAINT DF_PIE_ConfidenceLevel DEFAULT 'low',
        MatchReason         NVARCHAR(1000) NULL,
        ContentType         NVARCHAR(100)  NULL,
        ContentLengthBytes  BIGINT         NULL,
        ImageReachable      BIT            NULL,
        OldImageUrl         NVARCHAR(1000) NULL,
        CurrentImageStatus  NVARCHAR(40)   NULL,
        Status              NVARCHAR(50)   NOT NULL CONSTRAINT DF_PIE_Status DEFAULT 'pending',
        Provider            NVARCHAR(60)   NULL,
        License             NVARCHAR(120)  NULL,
        ApprovedByAdmin     BIT            NOT NULL CONSTRAINT DF_PIE_ApprovedByAdmin DEFAULT 0,
        FetchedAt           DATETIME2      NOT NULL CONSTRAINT DF_PIE_FetchedAt DEFAULT SYSUTCDATETIME(),
        ApprovedAt          DATETIME2      NULL,
        ApprovedByUserId    INT            NULL,
        AppliedAt           DATETIME2      NULL,
        ErrorMessage        NVARCHAR(1000) NULL,
        CreatedAt           DATETIME2      NOT NULL CONSTRAINT DF_PIE_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt          DATETIME2      NULL,
        CONSTRAINT FK_PartImageEnrichment_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartImageEnrichment_PartId ON dbo.PartImageEnrichment (PartId);
    CREATE INDEX IX_PartImageEnrichment_Status ON dbo.PartImageEnrichment (Status);
    CREATE INDEX IX_PartImageEnrichment_ConfidenceLevel ON dbo.PartImageEnrichment (ConfidenceLevel);
END;

IF COL_LENGTH('dbo.PartImageEnrichment', 'ThumbUrl') IS NULL ALTER TABLE dbo.PartImageEnrichment ADD ThumbUrl NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'Provider') IS NULL ALTER TABLE dbo.PartImageEnrichment ADD Provider NVARCHAR(60) NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'License') IS NULL ALTER TABLE dbo.PartImageEnrichment ADD License NVARCHAR(120) NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'CurrentImageStatus') IS NULL ALTER TABLE dbo.PartImageEnrichment ADD CurrentImageStatus NVARCHAR(40) NULL;
IF COL_LENGTH('dbo.PartImageEnrichment', 'ApprovedByAdmin') IS NULL ALTER TABLE dbo.PartImageEnrichment ADD ApprovedByAdmin BIT NOT NULL CONSTRAINT DF_PIE_ApprovedByAdmin DEFAULT 0;

IF OBJECT_ID('dbo.PartImageEnrichmentCandidates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartImageEnrichmentCandidates
    (
        Id                  INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartImageEnrichmentCandidates PRIMARY KEY,
        PartId              INT NOT NULL,
        CandidateRank       INT NOT NULL,
        ImageUrl            NVARCHAR(1000) NOT NULL,
        ThumbUrl            NVARCHAR(1000) NULL,
        SourcePageUrl       NVARCHAR(1000) NULL,
        SourceDomain        NVARCHAR(200) NULL,
        SearchQueryUsed     NVARCHAR(500) NULL,
        QueryTier           NVARCHAR(40) NULL,
        Provider            NVARCHAR(60) NULL,
        License             NVARCHAR(120) NULL,
        LicenseKnown        BIT NOT NULL CONSTRAINT DF_PIEC_LicenseKnown DEFAULT 0,
        ConfidenceScore     DECIMAL(5,2) NOT NULL CONSTRAINT DF_PIEC_ConfidenceScore DEFAULT 0,
        ConfidenceLevel     NVARCHAR(20) NOT NULL CONSTRAINT DF_PIEC_ConfidenceLevel DEFAULT 'low',
        MatchReason         NVARCHAR(1000) NULL,
        ImageReachable      BIT NULL,
        ContentType         NVARCHAR(100) NULL,
        ContentLengthBytes  BIGINT NULL,
        RejectionReason     NVARCHAR(200) NULL,
        Status              NVARCHAR(50) NOT NULL CONSTRAINT DF_PIEC_Status DEFAULT 'pending',
        FetchedAt           DATETIME2 NOT NULL CONSTRAINT DF_PIEC_FetchedAt DEFAULT SYSUTCDATETIME(),
        CreatedAt           DATETIME2 NOT NULL CONSTRAINT DF_PIEC_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_PartImageEnrichmentCandidates_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartImageEnrichmentCandidates_PartId ON dbo.PartImageEnrichmentCandidates (PartId);
END;

IF OBJECT_ID('dbo.PartOemEnrichment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartOemEnrichment
    (
        Id                         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartOemEnrichment PRIMARY KEY,
        PartId                     INT NOT NULL,
        ExistingOemNumber          NVARCHAR(200) NULL,
        ProposedPrimaryOemNumber   NVARCHAR(200) NULL,
        AlternativeOemNumbers      NVARCHAR(1000) NULL,
        ManufacturerPartNumber     NVARCHAR(200) NULL,
        SupplierPartNumber         NVARCHAR(200) NULL,
        SourceUrl                  NVARCHAR(1000) NULL,
        SourceDomain               NVARCHAR(200) NULL,
        SearchQueryUsed            NVARCHAR(500) NULL,
        SearchQueriesExecuted      INT NOT NULL CONSTRAINT DF_POE_SearchQueriesExecuted DEFAULT 0,
        TextResultsFound           INT NOT NULL CONSTRAINT DF_POE_TextResultsFound DEFAULT 0,
        ConfidenceScore            DECIMAL(5,2) NOT NULL CONSTRAINT DF_POE_ConfidenceScore DEFAULT 0,
        ConfidenceLevel            NVARCHAR(20) NOT NULL CONSTRAINT DF_POE_ConfidenceLevel DEFAULT 'low',
        MatchReason                NVARCHAR(1000) NULL,
        Status                     NVARCHAR(50) NOT NULL CONSTRAINT DF_POE_Status DEFAULT 'pending_review',
        FetchedAt                  DATETIME2 NOT NULL CONSTRAINT DF_POE_FetchedAt DEFAULT SYSUTCDATETIME(),
        ApprovedByAdmin            BIT NOT NULL CONSTRAINT DF_POE_ApprovedByAdmin DEFAULT 0,
        ErrorMessage               NVARCHAR(1000) NULL,
        CreatedAt                  DATETIME2 NOT NULL CONSTRAINT DF_POE_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt                 DATETIME2 NULL,
        CONSTRAINT FK_PartOemEnrichment_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartOemEnrichment_PartId ON dbo.PartOemEnrichment (PartId);
    CREATE INDEX IX_PartOemEnrichment_Status ON dbo.PartOemEnrichment (Status);
END;

IF OBJECT_ID('dbo.VehicleExpectedPartCandidates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.VehicleExpectedPartCandidates
    (
        Id                   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_VehicleExpectedPartCandidates PRIMARY KEY,
        DonorVehicleId       INT NOT NULL,
        ExpectedPartName     NVARCHAR(300) NOT NULL,
        Category             NVARCHAR(200) NULL,
        ExpectedOemNumbers   NVARCHAR(1000) NULL,
        CompatibleYearRange  NVARCHAR(100) NULL,
        Engine               NVARCHAR(200) NULL,
        BodyType             NVARCHAR(100) NULL,
        Drivetrain           NVARCHAR(100) NULL,
        SourceUrl            NVARCHAR(1000) NULL,
        ConfidenceScore      DECIMAL(5,2) NOT NULL CONSTRAINT DF_VEPC_ConfidenceScore DEFAULT 0,
        Status               NVARCHAR(50) NOT NULL CONSTRAINT DF_VEPC_Status DEFAULT 'CandidateFromVehicleSpec',
        CreatedAt            DATETIME2 NOT NULL CONSTRAINT DF_VEPC_CreatedAt DEFAULT SYSUTCDATETIME(),
        ApprovedByAdmin      BIT NOT NULL CONSTRAINT DF_VEPC_ApprovedByAdmin DEFAULT 0,
        Notes                NVARCHAR(1000) NULL,
        CONSTRAINT FK_VehicleExpectedPartCandidates_UsedCars FOREIGN KEY (DonorVehicleId) REFERENCES dbo.UsedCars (Id)
    );
    CREATE INDEX IX_VehicleExpectedPartCandidates_Vehicle ON dbo.VehicleExpectedPartCandidates (DonorVehicleId);
    CREATE INDEX IX_VehicleExpectedPartCandidates_Status ON dbo.VehicleExpectedPartCandidates (Status);
END;
""";

        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task UpsertEnrichmentRowAsync(SqlConnection conn, SqlTransaction tx, EnrichmentResult r)
    {
        const string sql = """
MERGE dbo.PartImageEnrichment AS target
USING (SELECT @PartId AS PartId) AS source
ON target.PartId = source.PartId
WHEN MATCHED THEN
    UPDATE SET
        SelectedImageUrl = @SelectedImageUrl,
        ThumbUrl = @ThumbUrl,
        SourcePageUrl = @SourcePageUrl,
        SourceDomain = @SourceDomain,
        SearchQueryUsed = @SearchQueryUsed,
        ConfidenceScore = @ConfidenceScore,
        ConfidenceLevel = @ConfidenceLevel,
        MatchReason = @MatchReason,
        ContentType = @ContentType,
        ContentLengthBytes = TRY_CONVERT(INT, @ContentLengthBytes),
        ImageReachable = @ImageReachable,
        OldImageUrl = @OldImageUrl,
        CurrentImageStatus = @CurrentImageStatus,
        Status = @Status,
        Provider = @Provider,
        License = @License,
        FetchedAt = SYSUTCDATETIME(),
        ErrorMessage = @ErrorMessage,
        ModifiedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (PartId, SelectedImageUrl, ThumbUrl, SourcePageUrl, SourceDomain, SearchQueryUsed,
            ConfidenceScore, ConfidenceLevel, MatchReason, ContentType, ContentLengthBytes,
            ImageReachable, OldImageUrl, CurrentImageStatus, Status, Provider, License,
            FetchedAt, ErrorMessage, CreatedAt)
    VALUES (@PartId, @SelectedImageUrl, @ThumbUrl, @SourcePageUrl, @SourceDomain, @SearchQueryUsed,
            @ConfidenceScore, @ConfidenceLevel, @MatchReason, @ContentType, TRY_CONVERT(INT, @ContentLengthBytes),
            @ImageReachable, @OldImageUrl, @CurrentImageStatus, @Status, @Provider, @License,
            SYSUTCDATETIME(), @ErrorMessage, SYSUTCDATETIME());
""";
        await using var cmd = new SqlCommand(sql, conn, tx);
        Add(cmd, "@PartId", r.PartId);
        Add(cmd, "@SelectedImageUrl", Truncate(r.SelectedImageUrl, 1000));
        Add(cmd, "@ThumbUrl", Truncate(r.ThumbUrl, 1000));
        Add(cmd, "@SourcePageUrl", Truncate(r.SourcePageUrl, 1000));
        Add(cmd, "@SourceDomain", Truncate(r.SourceDomain, 200));
        Add(cmd, "@SearchQueryUsed", Truncate(r.SearchQueryUsed, 500));
        Add(cmd, "@ConfidenceScore", r.ConfidenceScore);
        Add(cmd, "@ConfidenceLevel", r.ConfidenceLevel);
        Add(cmd, "@MatchReason", Truncate(r.MatchReason, 500));
        Add(cmd, "@ContentType", Truncate(r.ContentType, 100));
        Add(cmd, "@ContentLengthBytes", r.ContentLengthBytes);
        Add(cmd, "@ImageReachable", r.ImageReachable);
        Add(cmd, "@OldImageUrl", Truncate(r.OldImageUrl, 1000));
        Add(cmd, "@CurrentImageStatus", r.CurrentImageStatus);
        Add(cmd, "@Status", r.Status);
        Add(cmd, "@Provider", Truncate(r.Provider, 60));
        Add(cmd, "@License", Truncate(r.License, 120));
        Add(cmd, "@ErrorMessage", Truncate(r.ErrorMessage, 1000));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ReplaceImageCandidateRowsAsync(SqlConnection conn, SqlTransaction tx, IReadOnlyList<ImageCandidateAudit> candidates)
    {
        var partIds = candidates.Select(c => c.PartId).Distinct().ToList();
        foreach (var partId in partIds)
        {
            await using var delete = new SqlCommand("DELETE FROM dbo.PartImageEnrichmentCandidates WHERE PartId = @PartId;", conn, tx);
            Add(delete, "@PartId", partId);
            await delete.ExecuteNonQueryAsync();
        }

        const string sql = """
INSERT INTO dbo.PartImageEnrichmentCandidates
    (PartId, CandidateRank, ImageUrl, ThumbUrl, SourcePageUrl, SourceDomain, SearchQueryUsed,
     QueryTier, Provider, License, LicenseKnown, ConfidenceScore, ConfidenceLevel, MatchReason,
     ImageReachable, ContentType, ContentLengthBytes, RejectionReason, Status, FetchedAt, CreatedAt)
VALUES
    (@PartId, @CandidateRank, @ImageUrl, @ThumbUrl, @SourcePageUrl, @SourceDomain, @SearchQueryUsed,
     @QueryTier, @Provider, @License, @LicenseKnown, @ConfidenceScore, @ConfidenceLevel, @MatchReason,
     @ImageReachable, @ContentType, @ContentLengthBytes, @RejectionReason, @Status, SYSUTCDATETIME(), SYSUTCDATETIME());
""";

        foreach (var c in candidates)
        {
            await using var cmd = new SqlCommand(sql, conn, tx);
            Add(cmd, "@PartId", c.PartId);
            Add(cmd, "@CandidateRank", c.CandidateRank);
            Add(cmd, "@ImageUrl", Truncate(c.ImageUrl, 1000));
            Add(cmd, "@ThumbUrl", Truncate(c.ThumbUrl, 1000));
            Add(cmd, "@SourcePageUrl", Truncate(c.SourcePageUrl, 1000));
            Add(cmd, "@SourceDomain", Truncate(c.SourceDomain, 200));
            Add(cmd, "@SearchQueryUsed", Truncate(c.SearchQueryUsed, 500));
            Add(cmd, "@QueryTier", Truncate(c.QueryTier, 40));
            Add(cmd, "@Provider", Truncate(c.Provider, 60));
            Add(cmd, "@License", Truncate(c.License, 120));
            Add(cmd, "@LicenseKnown", c.LicenseKnown);
            Add(cmd, "@ConfidenceScore", c.ConfidenceScore);
            Add(cmd, "@ConfidenceLevel", c.ConfidenceLevel);
            Add(cmd, "@MatchReason", Truncate(c.MatchReason, 1000));
            Add(cmd, "@ImageReachable", c.ImageReachable);
            Add(cmd, "@ContentType", Truncate(c.ContentType, 100));
            Add(cmd, "@ContentLengthBytes", c.ContentLengthBytes);
            Add(cmd, "@RejectionReason", Truncate(c.RejectionReason, 200));
            Add(cmd, "@Status", c.Status);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static async Task UpsertOemEnrichmentRowAsync(SqlConnection conn, SqlTransaction tx, OemEnrichmentResult r)
    {
        const string sql = """
MERGE dbo.PartOemEnrichment AS target
USING (SELECT @PartId AS PartId) AS source
ON target.PartId = source.PartId
WHEN MATCHED THEN
    UPDATE SET
        ExistingOemNumber = @ExistingOemNumber,
        ProposedPrimaryOemNumber = @ProposedPrimaryOemNumber,
        AlternativeOemNumbers = @AlternativeOemNumbers,
        ManufacturerPartNumber = @ManufacturerPartNumber,
        SupplierPartNumber = @SupplierPartNumber,
        SourceUrl = @SourceUrl,
        SourceDomain = @SourceDomain,
        SearchQueryUsed = @SearchQueryUsed,
        SearchQueriesExecuted = @SearchQueriesExecuted,
        TextResultsFound = @TextResultsFound,
        ConfidenceScore = @ConfidenceScore,
        ConfidenceLevel = @ConfidenceLevel,
        MatchReason = @MatchReason,
        Status = @Status,
        FetchedAt = SYSUTCDATETIME(),
        ErrorMessage = @ErrorMessage,
        ModifiedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (PartId, ExistingOemNumber, ProposedPrimaryOemNumber, AlternativeOemNumbers,
            ManufacturerPartNumber, SupplierPartNumber, SourceUrl, SourceDomain, SearchQueryUsed,
            SearchQueriesExecuted, TextResultsFound, ConfidenceScore, ConfidenceLevel, MatchReason,
            Status, FetchedAt, ErrorMessage, CreatedAt)
    VALUES (@PartId, @ExistingOemNumber, @ProposedPrimaryOemNumber, @AlternativeOemNumbers,
            @ManufacturerPartNumber, @SupplierPartNumber, @SourceUrl, @SourceDomain, @SearchQueryUsed,
            @SearchQueriesExecuted, @TextResultsFound, @ConfidenceScore, @ConfidenceLevel, @MatchReason,
            @Status, SYSUTCDATETIME(), @ErrorMessage, SYSUTCDATETIME());
""";

        await using var cmd = new SqlCommand(sql, conn, tx);
        Add(cmd, "@PartId", r.PartId);
        Add(cmd, "@ExistingOemNumber", Truncate(r.ExistingOemNumber, 200));
        Add(cmd, "@ProposedPrimaryOemNumber", Truncate(r.ProposedPrimaryOemNumber, 200));
        Add(cmd, "@AlternativeOemNumbers", Truncate(r.AlternativeOemNumbers, 1000));
        Add(cmd, "@ManufacturerPartNumber", Truncate(r.ManufacturerPartNumber, 200));
        Add(cmd, "@SupplierPartNumber", Truncate(r.SupplierPartNumber, 200));
        Add(cmd, "@SourceUrl", Truncate(r.SourceUrl, 1000));
        Add(cmd, "@SourceDomain", Truncate(r.SourceDomain, 200));
        Add(cmd, "@SearchQueryUsed", Truncate(r.SearchQueryUsed, 500));
        Add(cmd, "@SearchQueriesExecuted", r.SearchQueriesExecuted);
        Add(cmd, "@TextResultsFound", r.TextResultsFound);
        Add(cmd, "@ConfidenceScore", r.ConfidenceScore);
        Add(cmd, "@ConfidenceLevel", r.ConfidenceLevel);
        Add(cmd, "@MatchReason", Truncate(r.MatchReason, 1000));
        Add(cmd, "@Status", r.Status);
        Add(cmd, "@ErrorMessage", Truncate(r.ErrorMessage, 1000));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ApplyImageToPartAsync(SqlConnection conn, SqlTransaction tx, int partId, string imageUrl)
    {
        const string sql = "UPDATE dbo.Parts SET ImageUrls = @ImageUrls, ModifiedAt = SYSUTCDATETIME() WHERE Id = @PartId;";
        await using var cmd = new SqlCommand(sql, conn, tx);
        Add(cmd, "@PartId", partId);
        Add(cmd, "@ImageUrls", JsonSerializer.Serialize(new[] { imageUrl }));
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task ApplyOemToPartAsync(SqlConnection conn, SqlTransaction tx, int partId, string oemNumber)
    {
        const string sql = "UPDATE dbo.Parts SET OEMNumber = @OEMNumber, ModifiedAt = SYSUTCDATETIME() WHERE Id = @PartId AND (OEMNumber IS NULL OR LTRIM(RTRIM(OEMNumber)) = '');";
        await using var cmd = new SqlCommand(sql, conn, tx);
        Add(cmd, "@PartId", partId);
        Add(cmd, "@OEMNumber", oemNumber);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task WriteMissingCandidateBatchAsync(SqlConnection conn, IReadOnlyList<MissingPartCandidate> candidates, RunLogger logger)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            var vehicleIds = candidates.Select(c => c.DonorVehicleId).Distinct().ToList();
            foreach (var vehicleId in vehicleIds)
            {
                await using var delete = new SqlCommand("DELETE FROM dbo.VehicleExpectedPartCandidates WHERE DonorVehicleId = @DonorVehicleId AND ApprovedByAdmin = 0;", conn, tx);
                Add(delete, "@DonorVehicleId", vehicleId);
                await delete.ExecuteNonQueryAsync();
            }

            const string sql = """
INSERT INTO dbo.VehicleExpectedPartCandidates
    (DonorVehicleId, ExpectedPartName, Category, ExpectedOemNumbers, CompatibleYearRange,
     Engine, BodyType, Drivetrain, SourceUrl, ConfidenceScore, Status, CreatedAt, ApprovedByAdmin, Notes)
VALUES
    (@DonorVehicleId, @ExpectedPartName, @Category, @ExpectedOemNumbers, @CompatibleYearRange,
     @Engine, @BodyType, @Drivetrain, @SourceUrl, @ConfidenceScore, @Status, SYSUTCDATETIME(), 0, @Notes);
""";

            foreach (var c in candidates)
            {
                await using var cmd = new SqlCommand(sql, conn, tx);
                Add(cmd, "@DonorVehicleId", c.DonorVehicleId);
                Add(cmd, "@ExpectedPartName", Truncate(c.ExpectedPartName, 300));
                Add(cmd, "@Category", Truncate(c.Category, 200));
                Add(cmd, "@ExpectedOemNumbers", Truncate(c.ExpectedOemNumbers, 1000));
                Add(cmd, "@CompatibleYearRange", Truncate(c.CompatibleYearRange, 100));
                Add(cmd, "@Engine", Truncate(c.Engine, 200));
                Add(cmd, "@BodyType", Truncate(c.BodyType, 100));
                Add(cmd, "@Drivetrain", Truncate(c.Drivetrain, 100));
                Add(cmd, "@SourceUrl", Truncate(c.SourceUrl, 1000));
                Add(cmd, "@ConfidenceScore", c.ConfidenceScore);
                Add(cmd, "@Status", c.Status);
                Add(cmd, "@Notes", Truncate(c.Notes, 1000));
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
            logger.Info($"Missing expected part candidate batch committed: {candidates.Count} rows.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.Error($"Missing expected part candidate batch rolled back: {ex.Message}");
        }
    }

    private static async Task<int> ApplyApprovedReviewCsvAsync(SqlConnection conn, string csvPath, RunLogger logger)
    {
        if (!File.Exists(csvPath))
        {
            throw new FileNotFoundException("Review CSV not found.", csvPath);
        }

        var rows = CsvReader.Read(csvPath)
            .Where(r => r.TryGetValue("ApprovedByAdmin", out var approved) && IsTrue(approved))
            .ToList();

        var applied = 0;
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        try
        {
            foreach (var row in rows)
            {
                if (!row.TryGetValue("PartId", out var partIdText) ||
                    !int.TryParse(partIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var partId) ||
                    !row.TryGetValue("SelectedImageUrl", out var imageUrl) ||
                    string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                await ApplyImageToPartAsync(conn, tx, partId, imageUrl);
                await MarkAdminApprovedAsync(conn, tx, partId);
                applied++;
            }

            await tx.CommitAsync();
            return applied;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.Error($"Review CSV apply rolled back: {ex.Message}");
            throw;
        }
    }

    private static async Task MarkAdminApprovedAsync(SqlConnection conn, SqlTransaction tx, int partId)
    {
        const string sql = """
UPDATE dbo.PartImageEnrichment
SET ApprovedByAdmin = 1,
    ApprovedAt = SYSUTCDATETIME(),
    AppliedAt = SYSUTCDATETIME(),
    Status = 'admin_approved',
    ModifiedAt = SYSUTCDATETIME()
WHERE PartId = @PartId;
""";
        await using var cmd = new SqlCommand(sql, conn, tx);
        Add(cmd, "@PartId", partId);
        await cmd.ExecuteNonQueryAsync();
    }

    internal static void Add(SqlCommand cmd, string name, object? value)
    {
        cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= max ? value : value[..max];
    }

    private static bool IsTrue(string? value)
    {
        return value is not null &&
               (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("approved", StringComparison.OrdinalIgnoreCase));
    }

    private static PathInfo FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "SpareParts.sln")))
            {
                return new PathInfo(dir.FullName);
            }

            dir = dir.Parent;
        }

        return new PathInfo(Directory.GetCurrentDirectory());
    }

    internal static string NormalizeIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return NonAlphaNumericRegex().Replace(value.ToLowerInvariant(), string.Empty);
    }

    internal static string DomainFromUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.Host.ToLowerInvariant() : string.Empty;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    internal static partial Regex NonAlphaNumericRegex();

    [GeneratedRegex("[a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    internal static partial Regex TokenRegex();
}

internal sealed record PathInfo(string FullName)
{
    public static PathInfo operator /(PathInfo left, string right) => new(Path.Combine(left.FullName, right));
    public bool Exists => Directory.Exists(FullName) || File.Exists(FullName);
    public override string ToString() => FullName;
}
