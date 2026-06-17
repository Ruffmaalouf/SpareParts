namespace SpareParts.Domain.ApiPlatform;

public class ApiKeyEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? Scopes { get; set; }
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
