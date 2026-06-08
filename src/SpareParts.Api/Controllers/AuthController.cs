using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpareParts.Api.Hosting;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Services;
using SpareParts.Domain.Auth;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _service;
        private readonly IHostEnvironment _hostEnvironment;

        public AuthController(AuthService service, IHostEnvironment hostEnvironment)
        {
            _service = service;
            _hostEnvironment = hostEnvironment;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting(SparePartsApiComposition.AuthRateLimitPolicy)]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
            => Ok(_service.Login(req));

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
