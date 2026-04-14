using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/usedcars")]
    [Authorize]
    public class UsedCarsController : SparePartsControllerBase
    {
        private readonly UsedCarsService _service;

        public UsedCarsController(UsedCarsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UsedCarDto>> GetAll() => Ok(_service.GetAll());

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateUsedCarRequest request)
            => Ok(_service.Create(request, CurrentUserId));

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Update(int id, [FromBody] CreateUsedCarRequest request)
        {
            _service.Update(id, request, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
