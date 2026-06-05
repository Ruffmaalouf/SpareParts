using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/appconstants")]
    [Authorize]
    public class AppConstantsController : ControllerBase
    {
        private readonly AppConstantsService _service;

        public AppConstantsController(AppConstantsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<AppConstantDto>> GetAll()
            => Ok(_service.GetAll());

        [HttpPut("{key}")]
        public IActionResult Upsert(string key, [FromBody] UpsertAppConstantRequest request)
        {
            _service.Upsert(key, request);
            return NoContent();
        }
    }
}
