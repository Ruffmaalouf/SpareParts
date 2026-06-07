namespace SpareParts.Domain.Auth
{
    public sealed class MeResponse
    {
        public string? UserId { get; init; }
        public string? FullName { get; init; }
        public string? RoleId { get; init; }
        public string? TenantId { get; init; }
        public string? TenantCode { get; init; }
    }
}
