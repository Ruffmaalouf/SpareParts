namespace SpareParts.Domain.Trust;

public class ContentReport
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public int TargetId { get; set; }
    public int ReportedByUserId { get; set; }
    public FraudCategory Category { get; set; }
    public string? Details { get; set; }
    public ReportStatus Status { get; set; }
    public bool AutoFlagged { get; set; }
    public DateTime CreatedAt { get; set; }
}
