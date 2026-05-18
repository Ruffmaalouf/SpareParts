namespace SpareParts.Domain.Reports;

public sealed class SaveReportDefinitionRequest
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SavedReportLayoutDto Layout { get; set; } = new();
    public bool IsFavorite { get; set; }
    public bool IsSensitive { get; set; }
    public string DefaultExportFormat { get; set; } = "xls";
    public string PreferredChartType { get; set; } = "bar";
    public List<ReportSavedReportRoleAccessDto> AccessRules { get; set; } = new();
}
