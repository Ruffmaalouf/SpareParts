namespace SpareParts.Domain.Reports;

public sealed class ReportChartLayoutDto
{
    public string ChartType { get; set; } = "bar";
    public string? CategoryColumnKey { get; set; }
    public string? ValueColumnKey { get; set; }
    public string? SecondaryValueColumnKey { get; set; }
}
