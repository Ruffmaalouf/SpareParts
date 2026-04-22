using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/parts")]
    [Authorize]
    public class PartsController : SparePartsControllerBase
    {
        private readonly PartsService _service;

        public PartsController(PartsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PartDto>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] int? usedCarId = null)
        {
            (page, pageSize) = NormalizePagination(page, pageSize);

            var result = _service.GetAll(page, pageSize, usedCarId);
            ApplyPaginationHeaders(page, pageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreatePartRequest req)
            => Ok(_service.Create(req, CurrentUserId));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreatePartRequest req)
        {
            _service.Update(id, req, CurrentUserId);
            return NoContent();
        }

        [HttpPut("{id:int}/usedcar")]
        public IActionResult UpdateUsedCar(int id, [FromBody] UpdatePartUsedCarRequest req)
        {
            _service.UpdateUsedCar(id, req.UsedCarId, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }

        [HttpPost("ai/notes")]
        public async Task<ActionResult<GeneratePartNotesResponse>> GenerateNotes(
            [FromBody] GeneratePartNotesRequest request,
            CancellationToken cancellationToken)
            => Ok(await _service.GenerateNotesAsync(request, cancellationToken));
    }
}
