using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/locations")]
    [Authorize]
    public class LocationsController : SparePartsControllerBase
    {
        private readonly LocationsService _service;

        public LocationsController(LocationsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<LocationDto>> GetAll() => Ok(_service.GetAll());

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateLocationRequest request)
            => Ok(_service.Create(request, CurrentUserId));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateLocationRequest request)
        {
            _service.Update(id, request, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
