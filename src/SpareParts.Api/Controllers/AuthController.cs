using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest req)
            => Ok(_service.Login(req));

        [HttpPost("external-login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> ExternalLogin(
            [FromBody] ExternalLoginRequest req,
            CancellationToken cancellationToken)
            => Ok(await _service.ExternalLoginAsync(req, cancellationToken));

        [HttpGet("me")]
        [Authorize]
        public ActionResult GetMe() => Ok(new
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            FullName = User.FindFirst(ClaimTypes.Name)?.Value,
            Role = User.FindFirst(ClaimTypes.Role)?.Value
        });

        [HttpGet("hashpassword")]
        [Authorize(Roles = "Admin")]
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
