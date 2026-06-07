namespace SpareParts.Infrastructure.Interfaces;

public interface ITenantContext
{
    int TenantId { get; }
    string TenantCode { get; }
    bool IsSuperAdmin { get; }
    bool IsResolved { get; }
}
