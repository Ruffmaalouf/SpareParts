using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class TenantContext : ITenantContext
{
    public int TenantId { get; set; }
    public string TenantCode { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public bool IsResolved { get; set; }

    public static ITenantContext Legacy { get; } = new TenantContext
    {
        TenantId = 0,
        TenantCode = string.Empty,
        IsSuperAdmin = false,
        IsResolved = true
    };
}
