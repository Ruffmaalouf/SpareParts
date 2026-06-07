using System.Security.Claims;
using SpareParts.Api.Infrastructure;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    public const string TenantIdClaimType = "tenant_id";
    public const string TenantCodeClaimType = "tenant_code";
    public const string TenantCodeHeaderName = "X-Tenant-Code";

    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, TenantsService tenantsService)
    {
        if (!context.User.Identity?.IsAuthenticated == true)
        {
            // Anonymous requests (login, health) — no tenant resolution needed.
            await _next(context);
            return;
        }

        var roleIdClaim = context.User.FindFirst(AuthorizationPolicies.RoleIdClaimType)?.Value;
        var isSuperAdmin = int.TryParse(roleIdClaim, out var roleId)
                           && roleId == (int)SpareParts.Domain.Auth.UserRole.SuperAdmin;

        var tenantIdClaim = context.User.FindFirst(TenantIdClaimType)?.Value;
        var tenantCodeClaim = context.User.FindFirst(TenantCodeClaimType)?.Value ?? string.Empty;

        if (isSuperAdmin && string.IsNullOrWhiteSpace(tenantIdClaim))
        {
            tenantContext.TenantId = 0;
            tenantContext.TenantCode = string.Empty;
            tenantContext.IsSuperAdmin = true;
            tenantContext.IsResolved = true;
            await _next(context);
            return;
        }

        if (!int.TryParse(tenantIdClaim, out var tenantId) || tenantId <= 0)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Tenant claim is missing or invalid.");
            return;
        }

        if (!tenantsService.ExistsAndActive(tenantId))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Tenant is not active or not found.");
            return;
        }

        tenantContext.TenantId = tenantId;
        tenantContext.TenantCode = tenantCodeClaim;
        tenantContext.IsSuperAdmin = false;
        tenantContext.IsResolved = true;

        await _next(context);
    }
}
