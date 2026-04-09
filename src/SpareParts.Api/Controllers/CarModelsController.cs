using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/carmodels")]
    [Authorize]
    public class CarModelsController : SparePartsControllerBase
    {
        private readonly CarModelsService _service;

        public CarModelsController(CarModelsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CarModelDto>> GetAll([FromQuery] int? brandId)
            => Ok(_service.GetAll(brandId));

        [HttpGet("{id:int}/image")]
        [AllowAnonymous]
        public ActionResult GetImage(int id)
        {
            var image = _service.GetImage(id);
            return File(image.Data, image.MimeType);
        }

        [HttpPost("{id:int}/image")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            if (image == null || image.Length == 0) return BadRequest("No image.");
            if (image.Length > 4 * 1024 * 1024) return BadRequest("Image must be ≤ 4 MB.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            _service.UploadImage(id, ms.ToArray(), image.ContentType);
            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateCarModelRequest req)
            => Ok(_service.Create(req, CurrentUserId));

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Update(int id, [FromBody] CreateCarModelRequest req)
        {
            _service.Update(id, req);
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
