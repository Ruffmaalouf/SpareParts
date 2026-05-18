namespace SpareParts.Domain.Reports;

public sealed class ReportJoinGraphDto
{
    public List<ReportJoinGraphNodeDto> Nodes { get; set; } = new();
    public List<ReportJoinGraphEdgeDto> Edges { get; set; } = new();
}
