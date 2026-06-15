using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpareParts.Api.Hosting;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Services;
using SpareParts.Domain.Auth;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private const string ClientPlatformHeader = "X-Client-Platform";
        private const string MobileClientPlatform = "mobile";

        private readonly AuthService _service;
        private readonly IHostEnvironment _hostEnvironment;
        private readonly ISubscriptionLimitService _subscriptionLimitService;

        public AuthController(AuthService service, IHostEnvironment hostEnvironment, ISubscriptionLimitService subscriptionLimitService)
        {
            _service = service;
            _hostEnvironment = hostEnvironment;
            _subscriptionLimitService = subscriptionLimitService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(SparePartsApiComposition.AuthRateLimitPolicy)]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
        {
            var response = _service.Login(req);
            EnsureMobileSellerAppAllowed(response);
            return Ok(response);
        }

        private void EnsureMobileSellerAppAllowed(LoginResponse response)
        {
            var isMobileClient = string.Equals(Request.Headers[ClientPlatformHeader].ToString(), MobileClientPlatform, StringComparison.OrdinalIgnoreCase);
            if (!isMobileClient)
            {
                return;
            }

            if (response.RoleId == AuthorizationPolicies.WebAppUserRoleId || response.RoleId == AuthorizationPolicies.SuperAdminRoleId)
            {
                return;
            }

            _subscriptionLimitService.EnsureFeatureEnabled(response.TenantId, FeatureCode.MobileSellerApp, "Mobile Seller App");
        }

        [HttpPost("external-login")]
        [AllowAnonymous]
        [EnableRateLimiting(SparePartsApiComposition.AuthRateLimitPolicy)]
        public async Task<ActionResult<LoginResponse>> ExternalLogin(
            [FromBody] ExternalLoginRequest req,
            CancellationToken cancellationToken)
            => Ok(await _service.ExternalLoginAsync(req, cancellationToken));

        [HttpGet("me")]
        [Authorize]
        public ActionResult<MeResponse> GetMe() => Ok(new MeResponse
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            FullName = User.FindFirst(ClaimTypes.Name)?.Value,
            RoleId = User.FindFirst(AuthorizationPolicies.RoleIdClaimType)?.Value,
            TenantId = User.FindFirst(SpareParts.Api.Middleware.TenantResolutionMiddleware.TenantIdClaimType)?.Value,
            TenantCode = User.FindFirst(SpareParts.Api.Middleware.TenantResolutionMiddleware.TenantCodeClaimType)?.Value
        });

        [HttpGet("hashpassword")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        [ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult HashPassword([FromQuery] string plain)
        {
            if (!_hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(plain))
            {
                return BadRequest("?plain= is required");
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(plain.Trim(), workFactor: 12);
            return Ok(new
            {
                hash
            });
        }
    }
}
