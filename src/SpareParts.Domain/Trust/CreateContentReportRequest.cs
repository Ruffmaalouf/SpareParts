namespace SpareParts.Domain.Trust;

public class CreateContentReportRequest
{
    public ReportTargetType TargetType { get; set; }
    public int TargetId { get; set; }
    public FraudCategory Category { get; set; }
    public string? Details { get; set; }
}
