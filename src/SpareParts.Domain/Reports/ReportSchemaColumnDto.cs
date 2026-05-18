namespace SpareParts.Domain.Reports;

public sealed class ReportSchemaColumnDto
{
    public string ColumnName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SqlDataType { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsForeignKey { get; set; }
    public int? MaxLength { get; set; }
    public string SampleValuesText { get; set; } = string.Empty;
}
