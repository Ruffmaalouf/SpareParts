namespace SpareParts.Domain.Reports;

public class ReportSavedReportSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TableKey { get; set; } = string.Empty;
    public string TableDisplayName { get; set; } = string.Empty;
    public bool IsFavorite { get; set; }
    public bool IsSensitive { get; set; }
    public string DefaultExportFormat { get; set; } = "xls";
    public string PreferredChartType { get; set; } = "bar";
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanExport { get; set; }
}
