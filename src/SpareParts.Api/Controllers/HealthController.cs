using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Hosting;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly ServiceProfile? _serviceProfile;

        public HealthController(ServiceProfile? serviceProfile = null)
        {
            _serviceProfile = serviceProfile;
        }

        [HttpGet("api/health")]
        public ActionResult Get() => Ok(new
        {
            status = "ok",
            utc = DateTime.UtcNow,
            service = _serviceProfile?.ServiceName ?? "SpareParts.Api",
            capabilities = _serviceProfile?.Capabilities.Select(c => c.ToString()).OrderBy(c => c).ToArray() ?? []
        });
    }
}
