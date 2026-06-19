using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
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
        private readonly UsedCarImagesService _imagesService;
        private readonly UsedCarTwinService _twinService;

        public UsedCarsController(UsedCarsService service, UsedCarImagesService imagesService, UsedCarTwinService twinService)
        {
            _service = service;
            _imagesService = imagesService;
            _twinService = twinService;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UsedCarDto>> GetAll() => Ok(_service.GetAll());

        [HttpGet("{id:int}/twin")]
        public ActionResult<UsedCarTwinDto> GetTwin(int id) => Ok(_twinService.GetTwin(id));

        [HttpGet("{id:int}/state-events")]
        public ActionResult<IEnumerable<UsedCarStateEventDto>> GetStateEvents(int id)
            => Ok(_twinService.GetStateEvents(id));

        [HttpPost("{id:int}/state-events")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public ActionResult<UsedCarStateEventDto> CreateStateEvent(int id, [FromBody] CreateUsedCarStateEventRequest request)
            => Ok(_twinService.AddStateEvent(id, request, CurrentUserId));

        [HttpGet("wholesale-sales")]
        public ActionResult<IEnumerable<UsedCarWholesaleSaleDto>> GetWholesaleSales()
            => Ok(_service.GetWholesaleSales());

        [HttpGet("{id:int}/images")]
        public ActionResult<IEnumerable<UsedCarImageDto>> GetImages(int id)
            => Ok(_imagesService.GetAll(id));

        [HttpGet("{id:int}/listing-package")]
        public ActionResult<UsedCarListingPackageDto> GetListingPackage(int id)
            => Ok(_service.BuildListingPackage(id));

        [HttpPost("{id:int}/wholesale-sales")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public ActionResult<CreateUsedCarWholesaleSaleResponse> CreateWholesaleSale(
            int id,
            [FromBody] CreateUsedCarWholesaleSaleRequest request)
            => Ok(_service.CreateWholesaleSale(id, request, CurrentUserId));

        [HttpPost("{id:int}/images")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            if (image == null || image.Length == 0) return BadRequest("No image.");
            if (image.Length > 8 * 1024 * 1024) return BadRequest("Image must be <= 8 MB.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            _imagesService.Create(id, ms.ToArray(), image.ContentType, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("images/{imageId:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult DeleteImage(int imageId)
        {
            _imagesService.Delete(imageId);
            return NoContent();
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public ActionResult<int> Create([FromBody] CreateUsedCarRequest request)
            => Ok(_service.Create(request, CurrentUserId));

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult Update(int id, [FromBody] CreateUsedCarRequest request)
        {
            _service.Update(id, request, CurrentUserId);
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
