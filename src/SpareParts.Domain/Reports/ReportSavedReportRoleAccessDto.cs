namespace SpareParts.Domain.Reports;

public sealed class ReportSavedReportRoleAccessDto
{
    public string RoleName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanEdit { get; set; }
    public bool CanExport { get; set; }
}
