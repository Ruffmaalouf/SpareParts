namespace SpareParts.Domain.Reports;

public sealed class ReportBuilderColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SqlDataType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
    public int OrdinalPosition { get; set; }
}
