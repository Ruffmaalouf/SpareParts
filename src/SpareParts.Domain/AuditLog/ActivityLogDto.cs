using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.AuditLog;

public class ActivityLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? EntityDescription { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LogActivityRequest
{
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    [MaxLength(100)] public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    [MaxLength(500)] public string? EntityDescription { get; set; }
    [MaxLength(4000)] public string? OldValues { get; set; }
    [MaxLength(4000)] public string? NewValues { get; set; }
}
