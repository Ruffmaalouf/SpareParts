namespace SpareParts.Domain.Reports;

public sealed class ReportSchemaInspectorDto
{
    public string TableKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long EstimatedRowCount { get; set; }
    public List<ReportSchemaColumnDto> Columns { get; set; } = new();
    public List<ReportSchemaForeignKeyDto> Relations { get; set; } = new();
}
