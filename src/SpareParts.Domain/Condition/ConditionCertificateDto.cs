namespace SpareParts.Domain.Condition;

public class ConditionCertificateDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PartId { get; set; }
    public ConditionGrade Grade { get; set; }
    public string GradeLabel { get; set; } = string.Empty;
    public string? PhotoUrls { get; set; }
    public string? Notes { get; set; }
    public string CertificateNumber { get; set; } = string.Empty;
    public string ScanMethod { get; set; } = "Mock";
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
}
