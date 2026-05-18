namespace SpareParts.Domain.Reports;

public sealed class SaveReportTableLinkRequest
{
    public int? Id { get; set; }
    public string LinkName { get; set; } = string.Empty;
    public string SourceTableKey { get; set; } = string.Empty;
    public string SourceColumnName { get; set; } = string.Empty;
    public string TargetTableKey { get; set; } = string.Empty;
    public string TargetColumnName { get; set; } = string.Empty;
    public string JoinType { get; set; } = "LEFT";
}
