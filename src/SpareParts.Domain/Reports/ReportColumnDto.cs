namespace SpareParts.Domain.Reports;

public sealed class ReportColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string TableKey { get; set; } = string.Empty;
    public string TableDisplayName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string QualifiedDisplayName { get; set; } = string.Empty;
    public string QualifiedColumnName { get; set; } = string.Empty;
    public string SqlDataType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
    public int OrdinalPosition { get; set; }
}
