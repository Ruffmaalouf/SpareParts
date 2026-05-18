namespace SpareParts.Domain.Reports;

public sealed class ReportJoinGraphNodeDto
{
    public string TableKey { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ColumnCount { get; set; }
}
