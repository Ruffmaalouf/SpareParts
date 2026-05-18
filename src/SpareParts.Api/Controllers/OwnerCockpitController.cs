using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.OwnerCockpit;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/owner-cockpit")]
    [Authorize]
    public sealed class OwnerCockpitController : SparePartsControllerBase
    {
        private readonly OwnerCockpitService _service;

        public OwnerCockpitController(OwnerCockpitService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<OwnerCockpitDashboardDto> GetDashboard([FromQuery] DateTime? businessDate = null)
            => Ok(_service.GetDashboard(businessDate));
    }
}
