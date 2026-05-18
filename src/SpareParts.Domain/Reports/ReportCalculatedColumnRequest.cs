namespace SpareParts.Domain.Reports;

public sealed class ReportCalculatedColumnRequest
{
    public string Alias { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
}
