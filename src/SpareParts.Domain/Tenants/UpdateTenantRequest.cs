using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Tenants;

public sealed class UpdateTenantRequest
{
    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Domain { get; set; }

    public bool IsActive { get; set; }
}
