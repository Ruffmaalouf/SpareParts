using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        [HttpGet("api/health")]
        public ActionResult Get() => Ok(new { status = "ok", utc = DateTime.UtcNow });
    }
}
