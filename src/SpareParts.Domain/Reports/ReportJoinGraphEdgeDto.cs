namespace SpareParts.Domain.Reports;

public sealed class ReportJoinGraphEdgeDto
{
    public string LinkKey { get; set; } = string.Empty;
    public string LinkName { get; set; } = string.Empty;
    public string SourceTableKey { get; set; } = string.Empty;
    public string TargetTableKey { get; set; } = string.Empty;
    public string JoinType { get; set; } = string.Empty;
    public string CardinalityLabel { get; set; } = string.Empty;
    public bool MayDuplicateRows { get; set; }
    public string DuplicationWarning { get; set; } = string.Empty;
    public bool IsSystemDefined { get; set; }
}
