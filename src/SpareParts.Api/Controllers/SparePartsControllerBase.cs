using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;

namespace SpareParts.Api.Controllers;

public abstract class SparePartsControllerBase : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            }

            return userId;
        }
    }

    protected int CurrentRoleId
        => AuthorizationPolicies.GetRoleId(User)
           ?? throw new UnauthorizedAccessException("Role ID claim is missing.");

    protected static (int Page, int PageSize) NormalizePagination(int page, int pageSize, int maxPageSize = 500)
        => (Math.Max(1, page), Math.Clamp(pageSize, 1, maxPageSize));

    protected static (int Page, int PageSize, bool IsPaged) NormalizeOptionalPagination(
        int? page,
        int? pageSize,
        int maxPageSize = 500)
    {
        var isPaged = page.HasValue || pageSize.HasValue;
        return (
            Math.Max(1, page ?? 1),
            isPaged ? Math.Clamp(pageSize ?? 100, 1, maxPageSize) : int.MaxValue,
            isPaged);
    }

    protected void ApplyPaginationHeaders(int page, int pageSize, int totalCount)
    {
        Response.Headers["X-Page"] = page.ToString();
        Response.Headers["X-Page-Size"] = pageSize.ToString();
        Response.Headers["X-Total-Count"] = totalCount.ToString();
    }
}
