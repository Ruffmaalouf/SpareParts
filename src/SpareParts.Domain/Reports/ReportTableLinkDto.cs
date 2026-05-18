namespace SpareParts.Domain.Reports;

public sealed class ReportTableLinkDto
{
    public int? Id { get; set; }
    public string LinkKey { get; set; } = string.Empty;
    public string LinkName { get; set; } = string.Empty;
    public string SourceTableKey { get; set; } = string.Empty;
    public string SourceSchemaName { get; set; } = string.Empty;
    public string SourceTableName { get; set; } = string.Empty;
    public string SourceTableDisplayName { get; set; } = string.Empty;
    public string SourceColumnName { get; set; } = string.Empty;
    public string TargetTableKey { get; set; } = string.Empty;
    public string TargetSchemaName { get; set; } = string.Empty;
    public string TargetTableName { get; set; } = string.Empty;
    public string TargetTableDisplayName { get; set; } = string.Empty;
    public string TargetColumnName { get; set; } = string.Empty;
    public string JoinType { get; set; } = "LEFT";
    public bool IsSystemDefined { get; set; }
    public string Description { get; set; } = string.Empty;
    public string SourceCardinality { get; set; } = string.Empty;
    public string TargetCardinality { get; set; } = string.Empty;
    public string CardinalityLabel { get; set; } = string.Empty;
    public bool MayDuplicateRows { get; set; }
    public string DuplicationWarning { get; set; } = string.Empty;
}
