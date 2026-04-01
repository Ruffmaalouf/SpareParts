using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/warehouses")]
    [Authorize]
    public class WarehousesController : ControllerBase
    {
        private readonly WarehousesService _service;

        public WarehousesController(WarehousesService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<WarehouseDto>> GetAll() => Ok(_service.GetAll());

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateWarehouseRequest req) => Ok(_service.Create(req));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateWarehouseRequest req)
        {
            _service.Update(id, req);
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
