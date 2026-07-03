using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SpareParts.Api.Hosting;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Locks in the framework-level authorization fallback: every endpoint requires an authenticated
/// user unless it explicitly carries [AllowAnonymous]. This is the safety net behind
/// EveryApiController_ShouldRequireAuthorization_UnlessExplicitlyAllowListed — even if a new
/// controller/action ships without an explicit [Authorize] attribute, ASP.NET Core's authorization
/// middleware still requires authentication by default because of this FallbackPolicy.
/// </summary>
public class AuthorizationFallbackPolicyTests
{
    [Fact]
    public void AddSparePartsApiCore_ShouldRegisterFallbackPolicy_RequiringAuthenticatedUser()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Secret"] = "test-fallback-policy-secret-with-at-least-32-chars",
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Unused;Trusted_Connection=True;"
        });

        builder.AddSparePartsApiCore();

        using var provider = builder.Services.BuildServiceProvider();
        var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        Assert.NotNull(authorizationOptions.FallbackPolicy);

        var requirement = authorizationOptions.FallbackPolicy!.Requirements
            .OfType<DenyAnonymousAuthorizationRequirement>()
            .SingleOrDefault();

        Assert.NotNull(requirement);
    }
}
