using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/partrequests")]
    [Authorize]
    public sealed class PartRequestsController : SparePartsControllerBase
    {
        private readonly PartRequestsService _service;

        public PartRequestsController(PartRequestsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PartRequestDto>> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
            => Ok(_service.GetAll(status, search));

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreatePartRequestItemRequest request)
            => Ok(_service.Create(request, CurrentUserId));

        [HttpPut("{id:int}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] UpdatePartRequestStatusRequest request)
        {
            _service.UpdateStatus(id, request, CurrentUserId);
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
