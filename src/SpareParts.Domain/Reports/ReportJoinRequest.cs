namespace SpareParts.Domain.Reports;

public sealed class ReportJoinRequest
{
    public string? LinkKey { get; set; }
    public int SortOrder { get; set; }
}
