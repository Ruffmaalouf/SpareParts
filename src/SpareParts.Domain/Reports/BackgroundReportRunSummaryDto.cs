namespace SpareParts.Domain.Reports;

public sealed class BackgroundReportRunSummaryDto
{
    public int Id { get; set; }
    public string ReportName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? RowCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool ResultAvailable { get; set; }
    public bool CanExport { get; set; }
}
