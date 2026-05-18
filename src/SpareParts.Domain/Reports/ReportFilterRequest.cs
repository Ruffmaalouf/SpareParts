namespace SpareParts.Domain.Reports;

public sealed class ReportFilterRequest
{
    public string? ColumnKey { get; set; }
    public string? ColumnName { get; set; }
    public string? Operator { get; set; }
    public string? Value { get; set; }
    public string? ValueTo { get; set; }
}
