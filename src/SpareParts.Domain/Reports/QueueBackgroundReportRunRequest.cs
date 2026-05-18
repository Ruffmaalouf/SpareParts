namespace SpareParts.Domain.Reports;

public sealed class QueueBackgroundReportRunRequest
{
    public string ReportName { get; set; } = string.Empty;
    public SavedReportLayoutDto Layout { get; set; } = new();
}
