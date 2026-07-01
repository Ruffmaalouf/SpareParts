using System.Text;
using System.Text.Json;

namespace SpareParts.ImageEnrichment;

internal sealed record ReportExport(
    PathInfo ResultsCsv,
    PathInfo ImageCandidatesCsv,
    PathInfo OemCsv,
    PathInfo MissingPartsCsv,
    PathInfo ReviewCsv,
    PathInfo HtmlReport,
    PathInfo SummaryJson);

internal static class ReportWriter
{
    public static async Task<ReportExport> WriteAsync(
        PathInfo reportsDir,
        string stamp,
        EnrichmentOptions options,
        int totalActiveParts,
        int partsSelectedCount,
        int vehiclesScannedCount,
        IReadOnlyList<EnrichmentResult> results,
        IReadOnlyList<ImageCandidateAudit> imageCandidates,
        IReadOnlyList<OemEnrichmentResult> oemResults,
        IReadOnlyList<MissingPartCandidate> missingCandidates,
        BackupExport backup,
        PathInfo schemaReport)
    {
        var resultsCsv = reportsDir / $"image_enrichment_results_{stamp}.csv";
        var imageCandidatesCsv = reportsDir / $"image_enrichment_candidates_{stamp}.csv";
        var oemCsv = reportsDir / $"oem_enrichment_results_{stamp}.csv";
        var missingPartsCsv = reportsDir / $"missing_expected_parts_{stamp}.csv";
        var reviewCsv = reportsDir / $"image_enrichment_review_{stamp}.csv";
        var html = reportsDir / $"image_enrichment_report_{stamp}.html";
        var summaryJson = reportsDir / $"image_enrichment_summary_{stamp}.json";

        await CsvWriter.WriteObjectsAsync(resultsCsv, results);
        await CsvWriter.WriteObjectsAsync(imageCandidatesCsv, imageCandidates);
        await CsvWriter.WriteObjectsAsync(oemCsv, oemResults);
        await CsvWriter.WriteObjectsAsync(missingPartsCsv, missingCandidates);

        var reviewRows = new List<Dictionary<string, object?>>();
        reviewRows.AddRange(results
            .Where(r => r.Status == "pending_review")
            .Select(r => new Dictionary<string, object?>
            {
                ["ReviewType"] = "image",
                ["ApprovedByAdmin"] = "false",
                ["PartId"] = r.PartId,
                ["InternalCode"] = r.InternalCode,
                ["PartName"] = r.PartName,
                ["Vehicle"] = string.Join(" ", new[] { r.CarYear?.ToString(), r.CarMake, r.CarModel }.Where(v => !string.IsNullOrWhiteSpace(v))),
                ["OldImageUrl"] = r.OldImageUrl,
                ["SelectedImageUrl"] = r.SelectedImageUrl,
                ["ThumbUrl"] = r.ThumbUrl,
                ["ConfidenceScore"] = r.ConfidenceScore,
                ["ConfidenceLevel"] = r.ConfidenceLevel,
                ["SourcePageUrl"] = r.SourcePageUrl,
                ["SourceDomain"] = r.SourceDomain,
                ["Provider"] = r.Provider,
                ["License"] = r.License,
                ["MatchReason"] = r.MatchReason,
                ["ErrorMessage"] = r.ErrorMessage
            }));

        reviewRows.AddRange(oemResults
            .Where(r => r.Status == "pending_review")
            .Select(r => new Dictionary<string, object?>
            {
                ["ReviewType"] = "oem",
                ["ApprovedByAdmin"] = "false",
                ["PartId"] = r.PartId,
                ["InternalCode"] = r.InternalCode,
                ["PartName"] = r.PartName,
                ["Vehicle"] = r.Vehicle,
                ["ExistingOem"] = r.ExistingOemNumber,
                ["ProposedOem"] = r.ProposedPrimaryOemNumber,
                ["AlternativeOems"] = r.AlternativeOemNumbers,
                ["ConfidenceScore"] = r.ConfidenceScore,
                ["ConfidenceLevel"] = r.ConfidenceLevel,
                ["SourceUrl"] = r.SourceUrl,
                ["SourceDomain"] = r.SourceDomain,
                ["MatchReason"] = r.MatchReason,
                ["ErrorMessage"] = r.ErrorMessage
            }));

        reviewRows.AddRange(missingCandidates
            .Select(r => new Dictionary<string, object?>
            {
                ["ReviewType"] = "missing_expected_part",
                ["ApprovedByAdmin"] = "false",
                ["DonorVehicleId"] = r.DonorVehicleId,
                ["Vehicle"] = r.Vehicle,
                ["ExpectedPartName"] = r.ExpectedPartName,
                ["Category"] = r.Category,
                ["ExpectedOemNumbers"] = r.ExpectedOemNumbers,
                ["ConfidenceScore"] = r.ConfidenceScore,
                ["Status"] = r.Status,
                ["Notes"] = r.Notes
            }));

        await CsvWriter.WriteDictionariesAsync(reviewCsv, reviewRows);

        var report = new ReportExport(resultsCsv, imageCandidatesCsv, oemCsv, missingPartsCsv, reviewCsv, html, summaryJson);
        var summary = RunSummary.FromResults(totalActiveParts, partsSelectedCount, vehiclesScannedCount, results, imageCandidates, oemResults, missingCandidates, options, backup, report);
        await File.WriteAllTextAsync(summaryJson.FullName, JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(html.FullName, BuildHtml(stamp, options, totalActiveParts, partsSelectedCount, vehiclesScannedCount, results, imageCandidates, oemResults, missingCandidates, backup, schemaReport, summary), Encoding.UTF8);

        return report;
    }

    private static string BuildHtml(
        string stamp,
        EnrichmentOptions options,
        int totalActiveParts,
        int partsSelectedCount,
        int vehiclesScannedCount,
        IReadOnlyList<EnrichmentResult> results,
        IReadOnlyList<ImageCandidateAudit> imageCandidates,
        IReadOnlyList<OemEnrichmentResult> oemResults,
        IReadOnlyList<MissingPartCandidate> missingCandidates,
        BackupExport backup,
        PathInfo schemaReport,
        RunSummary summary)
    {
        var rows = new StringBuilder();
        foreach (var r in results)
        {
            rows.AppendLine($"""
<tr data-status="{H(r.Status)}" data-current="{H(r.CurrentImageStatus)}">
  <td>{r.PartId}</td>
  <td>
    <strong>{H(r.PartName)}</strong><br>
    <span>{H(r.InternalCode)}</span><br>
    <small>{H(string.Join(" ", new[] { r.CarYear?.ToString(), r.CarMake, r.CarModel }.Where(v => !string.IsNullOrWhiteSpace(v))))}</small>
  </td>
  <td><span class="badge current">{H(r.CurrentImageStatus)}</span><br><small>{H(r.CurrentImageReason)}</small></td>
  <td>{Img(r.OldImageUrl)}</td>
  <td>{Img(r.ThumbUrl ?? r.SelectedImageUrl)}</td>
  <td><span class="badge {H(r.ConfidenceLevel)}">{H(r.ConfidenceLevel)}</span><br><small>{r.ConfidenceScore:0.0}/100</small></td>
  <td><a href="{H(r.SourcePageUrl)}" target="_blank" rel="noreferrer">{H(r.SourceDomain ?? r.Provider ?? "")}</a><br><small>{H(r.License)}</small></td>
  <td><small>{H(r.MatchReason)}</small></td>
  <td><span class="badge status">{H(r.Status)}</span><br><small>{H(r.ErrorMessage)}</small></td>
</tr>
""");
        }

        var summaryCards = string.Join("", summary.Metrics.Select(kvp =>
            $"""<div class="card"><div class="num">{kvp.Value}</div><div class="label">{H(kvp.Key)}</div></div>"""));

        var candidateRows = new StringBuilder();
        foreach (var c in imageCandidates.Take(500))
        {
            candidateRows.AppendLine($"""
<tr>
  <td>{c.PartId}</td><td>{H(c.PartName)}<br><small>{H(c.Vehicle)}</small></td>
  <td>{c.CandidateRank}</td><td>{Img(c.ThumbUrl ?? c.ImageUrl)}</td>
  <td><a href="{H(c.SourcePageUrl)}" target="_blank" rel="noreferrer">{H(c.SourceDomain ?? c.Provider)}</a><br><small>{H(c.SearchQueryUsed)}</small></td>
  <td><span class="badge {H(c.ConfidenceLevel)}">{H(c.ConfidenceLevel)}</span><br><small>{c.ConfidenceScore:0.0}/100</small></td>
  <td><small>{H(c.MatchReason)}</small><br><small>{H(c.RejectionReason)}</small></td>
</tr>
""");
        }

        var oemRows = new StringBuilder();
        foreach (var o in oemResults.Take(500))
        {
            oemRows.AppendLine($"""
<tr>
  <td>{o.PartId}</td><td>{H(o.PartName)}<br><small>{H(o.Vehicle)}</small></td>
  <td>{H(o.ExistingOemNumber)}</td><td>{H(o.ProposedPrimaryOemNumber)}</td>
  <td><span class="badge {H(o.ConfidenceLevel)}">{H(o.ConfidenceLevel)}</span><br><small>{o.ConfidenceScore:0.0}/100</small></td>
  <td><a href="{H(o.SourceUrl)}" target="_blank" rel="noreferrer">{H(o.SourceDomain)}</a><br><small>{H(o.SearchQueryUsed)}</small></td>
  <td><span class="badge status">{H(o.Status)}</span><br><small>{H(o.MatchReason ?? o.ErrorMessage)}</small></td>
</tr>
""");
        }

        var missingRows = new StringBuilder();
        foreach (var m in missingCandidates.Take(500))
        {
            missingRows.AppendLine($"""
<tr>
  <td>{m.DonorVehicleId}</td><td>{H(m.Vehicle)}</td><td>{H(m.ExpectedPartName)}</td><td>{H(m.Category)}</td>
  <td><small>{H(m.Engine)} {H(m.BodyType)}</small></td>
  <td>{m.ConfidenceScore:0.0}</td><td><span class="badge status">{H(m.Status)}</span><br><small>{H(m.Notes)}</small></td>
</tr>
""");
        }

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>SpareParts Image Enrichment {{H(stamp)}}</title>
<style>
body{font-family:Segoe UI,Arial,sans-serif;margin:0;background:#f6f7f9;color:#18212f}
header{padding:24px 28px;background:#111827;color:#fff}
h1{font-size:22px;margin:0 0 6px}
.meta{color:#cbd5e1;font-size:13px}
main{padding:22px 28px}
.banner{border:1px solid #d7dee8;background:#fff;padding:14px 16px;margin-bottom:18px;border-radius:6px}
.dry{border-color:#d89b19;background:#fff8e6}
.live{border-color:#1e8e3e;background:#eaf8ee}
.summary{display:flex;gap:12px;flex-wrap:wrap;margin-bottom:18px}
.card{background:#fff;border:1px solid #dde3ec;border-radius:6px;padding:12px 14px;min-width:126px}
.num{font-size:25px;font-weight:700}
.label{font-size:12px;color:#5d6675}
.files{font-size:13px;line-height:1.65;margin-bottom:18px}
.filters{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:14px}
button{border:1px solid #c8d0dc;background:#fff;border-radius:5px;padding:7px 10px;cursor:pointer}
button.active{background:#18212f;color:#fff}
table{width:100%;border-collapse:collapse;background:#fff;border:1px solid #dde3ec}
th{position:sticky;top:0;background:#253044;color:#fff;text-align:left;font-size:12px;padding:9px}
td{border-top:1px solid #edf0f4;padding:9px;vertical-align:top;font-size:13px}
small{color:#667085}
img{width:118px;height:86px;object-fit:cover;border:1px solid #d7dee8;border-radius:4px;background:#f2f4f7}
.badge{display:inline-block;border-radius:999px;padding:2px 8px;font-size:11px;font-weight:700;background:#e5e7eb;color:#1f2937}
.high{background:#dcfce7;color:#166534}.medium{background:#fef3c7;color:#92400e}.low{background:#fee2e2;color:#991b1b}
.status{background:#e0e7ff;color:#3730a3}.current{background:#f3f4f6;color:#374151}
a{color:#155eef;text-decoration:none;word-break:break-all}
</style>
</head>
<body>
<header>
  <h1>SpareParts Image Enrichment Report</h1>
  <div class="meta">Generated {{H(stamp)}} - {{(options.DryRun ? "DRY RUN" : "LIVE")}} - selected {{partsSelectedCount}} of {{totalActiveParts}} active parts; vehicles scanned {{vehiclesScannedCount}}</div>
</header>
<main>
  <div class="banner {{(options.DryRun ? "dry" : "live")}}">
    {{(options.DryRun ? "Dry-run mode: no database tables or image URLs were changed." : "Live mode: metadata was written and only auto-approved high-confidence images were applied.")}}
  </div>
  <section class="summary">{{summaryCards}}</section>
  <section class="files">
    <strong>Generated files</strong><br>
    Results CSV: {{H(summary.ResultsCsv)}}<br>
    Image candidates CSV: {{H(summary.ImageCandidatesCsv)}}<br>
    OEM CSV: {{H(summary.OemCsv)}}<br>
    Missing expected parts CSV: {{H(summary.MissingPartsCsv)}}<br>
    Review CSV: {{H(summary.ReviewCsv)}}<br>
    Current parts/images CSV: {{H(backup.CurrentPartsReportCsv.FullName)}}<br>
    Backup CSV: {{H(backup.PartsCsv.FullName)}}<br>
    OEM/part-number backup CSV: {{H(backup.OemPartNumbersCsv.FullName)}}<br>
    Donor vehicle backup CSV: {{H(backup.DonorVehiclesCsv.FullName)}}<br>
    Part compatibility backup CSV: {{H(backup.PartCompatibilitiesCsv?.FullName)}}<br>
    Rollback SQL: {{H(backup.RollbackSql.FullName)}}<br>
    Schema report: {{H(schemaReport.FullName)}}
  </section>
  <div class="filters">
    <button class="active" onclick="filterRows('all')">All</button>
    <button onclick="filterRows('auto_approved')">Auto-approved</button>
    <button onclick="filterRows('pending_review')">Pending review</button>
    <button onclick="filterRows('skipped')">Skipped</button>
    <button onclick="filterRows('failed')">Failed</button>
    <button onclick="filterRows('duplicated')">Current duplicated</button>
    <button onclick="filterRows('broken')">Current broken</button>
  </div>
  <table id="results">
    <thead>
      <tr>
        <th>ID</th><th>Part</th><th>Current Status</th><th>Old Image</th><th>Proposed Image</th>
        <th>Confidence</th><th>Source</th><th>Reason</th><th>Status</th>
      </tr>
    </thead>
    <tbody>
      {{rows}}
    </tbody>
  </table>
  <h2>Top Image Candidates</h2>
  <table>
    <thead><tr><th>ID</th><th>Part</th><th>Rank</th><th>Candidate</th><th>Source</th><th>Confidence</th><th>Reason</th></tr></thead>
    <tbody>{{candidateRows}}</tbody>
  </table>
  <h2>OEM Candidates</h2>
  <table>
    <thead><tr><th>ID</th><th>Part</th><th>Existing OEM</th><th>Proposed OEM</th><th>Confidence</th><th>Source</th><th>Status</th></tr></thead>
    <tbody>{{oemRows}}</tbody>
  </table>
  <h2>Missing Expected Parts</h2>
  <table>
    <thead><tr><th>Vehicle ID</th><th>Vehicle</th><th>Expected Part</th><th>Category</th><th>Spec</th><th>Confidence</th><th>Status</th></tr></thead>
    <tbody>{{missingRows}}</tbody>
  </table>
</main>
<script>
function filterRows(status){
  document.querySelectorAll('button').forEach(b=>b.classList.remove('active'));
  event.target.classList.add('active');
  document.querySelectorAll('#results tbody tr').forEach(row=>{
    const match = status === 'all' || row.dataset.status === status || row.dataset.current === status;
    row.style.display = match ? '' : 'none';
  });
}
</script>
</body>
</html>
""";
    }

    private static string Img(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "<small>no image</small>";
        }

        return $"""<a href="{H(url)}" target="_blank" rel="noreferrer"><img src="{H(url)}" loading="lazy" onerror="this.replaceWith(document.createTextNode('image failed'))"></a>""";
    }

    private static string H(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal);
    }
}

internal sealed record RunSummary
{
    public Dictionary<string, int> Metrics { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public string Mode { get; init; } = "";
    public int TotalActiveParts { get; init; }
    public int TotalPartsScanned { get; init; }
    public string ResultsCsv { get; init; } = "";
    public string ImageCandidatesCsv { get; init; } = "";
    public string OemCsv { get; init; } = "";
    public string MissingPartsCsv { get; init; } = "";
    public string ReviewCsv { get; init; } = "";
    public string HtmlReport { get; init; } = "";
    public string RollbackSql { get; init; } = "";
    public string PartsBackupCsv { get; init; } = "";

    public static RunSummary FromResults(
        int totalActiveParts,
        int partsSelectedCount,
        int vehiclesScannedCount,
        IReadOnlyList<EnrichmentResult> results,
        IReadOnlyList<ImageCandidateAudit> imageCandidates,
        IReadOnlyList<OemEnrichmentResult> oemResults,
        IReadOnlyList<MissingPartCandidate> missingCandidates,
        EnrichmentOptions options,
        BackupExport backup,
        ReportExport report)
    {
        var metrics = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["total_active_parts"] = totalActiveParts,
            ["total_parts_scanned"] = results.Count > 0 ? results.Count : Math.Max(partsSelectedCount, oemResults.Count),
            ["existing_images_found"] = results.Count(r => !string.IsNullOrWhiteSpace(r.OldImageUrl)),
            ["existing_suspicious_or_wrong"] = results.Count(r => r.CurrentImageStatus is "suspicious" or "wrong" or "duplicated" or "placeholder"),
            ["broken_images"] = results.Count(r => r.CurrentImageStatus == "broken"),
            ["duplicate_images"] = results.Count(r => r.CurrentImageStatus == "duplicated"),
            ["missing_images"] = results.Count(r => r.CurrentImageStatus == "missing"),
            ["image_search_queries_executed"] = results.Sum(r => r.SearchQueriesExecuted),
            ["candidate_images_found"] = results.Sum(r => r.CandidateImagesFound),
            ["candidate_images_rejected"] = results.Sum(r => r.CandidateImagesRejected),
            ["high_confidence_replacements"] = results.Count(r => r.ConfidenceLevel == "high" && r.Status == "auto_approved"),
            ["medium_confidence_review"] = results.Count(r => r.Status == "pending_review"),
            ["skipped_parts"] = results.Count(r => r.Status == "skipped"),
            ["failed_parts"] = results.Count(r => r.Status == "failed"),
            ["top_image_candidates_saved"] = imageCandidates.Count,
            ["oem_parts_scanned"] = oemResults.Count,
            ["oem_numbers_already_existing"] = oemResults.Count(r => r.Status == "existing"),
            ["oem_numbers_found"] = oemResults.Count(r => !string.IsNullOrWhiteSpace(r.ProposedPrimaryOemNumber) && r.Status != "existing"),
            ["oem_numbers_updated"] = options.DryRun ? 0 : oemResults.Count(r => r.Status == "verified" && string.IsNullOrWhiteSpace(r.ExistingOemNumber)),
            ["oem_numbers_pending_review"] = oemResults.Count(r => r.Status == "pending_review"),
            ["oem_conflicts_found"] = oemResults.Count(r => !string.IsNullOrWhiteSpace(r.ExistingOemNumber) &&
                                                           !string.IsNullOrWhiteSpace(r.ProposedPrimaryOemNumber) &&
                                                           !r.ExistingOemNumber.Equals(r.ProposedPrimaryOemNumber, StringComparison.OrdinalIgnoreCase)),
            ["failed_oem_lookups"] = oemResults.Count(r => r.Status == "failed"),
            ["vehicles_scanned"] = vehiclesScannedCount,
            ["expected_missing_parts_detected"] = missingCandidates.Count,
            ["candidate_missing_parts_inserted"] = options.DryRun ? 0 : missingCandidates.Count,
            ["missing_parts_needing_manual_approval"] = missingCandidates.Count
        };

        return new RunSummary
        {
            Metrics = metrics,
            Mode = options.DryRun ? "dry-run" : "live",
            TotalActiveParts = totalActiveParts,
            TotalPartsScanned = results.Count > 0 ? results.Count : Math.Max(partsSelectedCount, oemResults.Count),
            ResultsCsv = report.ResultsCsv.FullName,
            ImageCandidatesCsv = report.ImageCandidatesCsv.FullName,
            OemCsv = report.OemCsv.FullName,
            MissingPartsCsv = report.MissingPartsCsv.FullName,
            ReviewCsv = report.ReviewCsv.FullName,
            HtmlReport = report.HtmlReport.FullName,
            RollbackSql = backup.RollbackSql.FullName,
            PartsBackupCsv = backup.PartsCsv.FullName
        };
    }

    public IEnumerable<string> ToLines()
    {
        foreach (var kvp in Metrics)
        {
            yield return $"{kvp.Key}: {kvp.Value}";
        }

        yield return $"results_csv: {ResultsCsv}";
        yield return $"image_candidates_csv: {ImageCandidatesCsv}";
        yield return $"oem_csv: {OemCsv}";
        yield return $"missing_parts_csv: {MissingPartsCsv}";
        yield return $"review_csv: {ReviewCsv}";
        yield return $"html_report: {HtmlReport}";
        yield return $"rollback_sql: {RollbackSql}";
    }
}
