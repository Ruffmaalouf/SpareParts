using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Tenants;

public sealed class CreateTenantRequest
{
    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Domain { get; set; }
}
