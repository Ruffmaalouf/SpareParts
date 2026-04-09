using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = "Admin,Manager")]
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
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateCarBrandRequest req)
            => Ok(_service.Create(req, CurrentUserId));
    }
}
