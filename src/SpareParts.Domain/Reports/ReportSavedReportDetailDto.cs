namespace SpareParts.Domain.Reports;

public sealed class ReportSavedReportDetailDto : ReportSavedReportSummaryDto
{
    public SavedReportLayoutDto Layout { get; set; } = new();
    public List<ReportSavedReportRoleAccessDto> AccessRules { get; set; } = new();
}
