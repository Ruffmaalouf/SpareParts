using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Domain.Auth;

namespace SpareParts.Api.Infrastructure;

public static class AuthorizationPolicies
{
    public const string RoleIdClaimType = "roleId";
    public const string Admin = "role-id:admin";
    public const string AdminOrManager = "role-id:admin-or-manager";
    public const string WebAppUser = "role-id:web-app-user";

    public const int AdminRoleId = (int)UserRole.Admin;
    public const int ManagerRoleId = (int)UserRole.Manager;
    public const int CashierRoleId = (int)UserRole.Cashier;
    public const int WebAppUserRoleId = WebAppUserRoleMigration.RoleId;

    public static void AddRoleIdPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, policy =>
            policy.RequireAssertion(context => HasAnyRoleId(context.User, AdminRoleId)));

        options.AddPolicy(AdminOrManager, policy =>
            policy.RequireAssertion(context => HasAnyRoleId(context.User, AdminRoleId, ManagerRoleId)));

        options.AddPolicy(WebAppUser, policy =>
            policy.RequireAssertion(context => HasAnyRoleId(context.User, WebAppUserRoleId)));
    }

    public static bool HasAnyRoleId(ClaimsPrincipal user, params int[] roleIds)
    {
        var claim = user.FindFirst(RoleIdClaimType)?.Value;
        return int.TryParse(claim, out var roleId) && roleIds.Contains(roleId);
    }

    public static int? GetRoleId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(RoleIdClaimType)?.Value;
        return int.TryParse(claim, out var roleId) ? roleId : null;
    }
}
