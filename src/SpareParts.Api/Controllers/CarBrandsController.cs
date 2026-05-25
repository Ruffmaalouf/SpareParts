using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/carbrands")]
    [Authorize]
    public class CarBrandsController : SparePartsControllerBase
    {
        private readonly CarBrandsService _service;

        public CarBrandsController(CarBrandsService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CarBrandDto>> GetAll() => Ok(_service.GetAll());

        [HttpGet("{id:int}/logo")]
        [AllowAnonymous]
        public ActionResult GetLogo(int id)
        {
            var logo = _service.GetLogo(id);
            return File(logo.Data, logo.MimeType);
        }

        [HttpPost("{id:int}/logo")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> UploadLogo(int id, IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image provided.");
            }

            if (image.Length > 2 * 1024 * 1024)
            {
                return BadRequest("Image must be ≤ 2 MB.");
            }

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            _service.UploadLogo(id, ms.ToArray(), image.ContentType);
            return NoContent();
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public ActionResult<int> Create([FromBody] CreateCarBrandRequest req)
            => Ok(_service.Create(req, CurrentUserId));

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult Update(int id, [FromBody] CreateCarBrandRequest req)
        {
            _service.Update(id, req);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
