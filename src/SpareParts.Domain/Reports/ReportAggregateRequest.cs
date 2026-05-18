namespace SpareParts.Domain.Reports;

public sealed class ReportAggregateRequest
{
    public string AggregateType { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string? SourceIdentifier { get; set; }
}
