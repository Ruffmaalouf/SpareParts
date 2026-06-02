using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/brands")]
    [Authorize]
    public class BrandsController : SparePartsControllerBase
    {
        private readonly BrandsService _service;

        public BrandsController(BrandsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<BrandDto>> GetAll([FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {
            var pagination = NormalizeOptionalPagination(page, pageSize);
            var result = _service.GetAll(pagination.Page, pagination.PageSize);
            if (pagination.IsPaged)
            {
                ApplyPaginationHeaders(pagination.Page, pagination.PageSize, result.TotalCount);
            }

            return Ok(result.Items);
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateBrandRequest req)
            => Ok(_service.Create(req, CurrentUserId));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateBrandRequest req)
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
