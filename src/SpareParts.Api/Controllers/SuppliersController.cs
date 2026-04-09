using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    [Authorize]
    public class SuppliersController : SparePartsControllerBase
    {
        private readonly SuppliersService _service;

        public SuppliersController(SuppliersService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<SupplierDto>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            (page, pageSize) = NormalizePagination(page, pageSize);

            var result = _service.GetAll(page, pageSize);
            ApplyPaginationHeaders(page, pageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateSupplierRequest req)
            => Ok(_service.Create(req, CurrentUserId));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateSupplierRequest req)
        {
            _service.Update(id, req, CurrentUserId);
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
