namespace SpareParts.Domain.Reports;

public sealed class ReportSchemaForeignKeyDto
{
    public string LinkName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string RelatedTableKey { get; set; } = string.Empty;
    public string RelatedTableDisplayName { get; set; } = string.Empty;
    public string RelatedColumnName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string CardinalityLabel { get; set; } = string.Empty;
}
