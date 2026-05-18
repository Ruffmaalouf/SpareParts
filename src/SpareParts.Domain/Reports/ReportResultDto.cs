namespace SpareParts.Domain.Reports;

public sealed class ReportResultDto
{
    public string TableName { get; set; } = string.Empty;
    public List<ReportResultColumnDto> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public int MaxRows { get; set; }
    public bool IsGrouped { get; set; }
    public string GeneratedSql { get; set; } = string.Empty;
}
