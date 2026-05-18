namespace SpareParts.Domain.Reports;

public sealed class SavedReportLayoutDto
{
    public string? TableKey { get; set; }
    public List<ReportJoinRequest> Joins { get; set; } = new();
    public List<string> Columns { get; set; } = new();
    public List<string> GroupByColumns { get; set; } = new();
    public List<ReportCalculatedColumnRequest> CalculatedColumns { get; set; } = new();
    public List<ReportAggregateRequest> Aggregates { get; set; } = new();
    public List<ReportFilterRequest> Filters { get; set; } = new();
    public string? SortColumn { get; set; }
    public bool SortDescending { get; set; }
    public int MaxRows { get; set; } = 500;
    public bool IncludeSqlPreview { get; set; } = true;
    public ReportChartLayoutDto ChartLayout { get; set; } = new();
    public ReportCurrencySettingsDto CurrencySettings { get; set; } = new();
}
