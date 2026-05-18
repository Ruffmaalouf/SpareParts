namespace SpareParts.Domain.Reports;

public sealed class ReportResultColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
}
