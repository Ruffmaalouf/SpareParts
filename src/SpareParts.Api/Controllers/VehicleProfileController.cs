using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.VehicleProfile;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/vehicle-profile")]
[Authorize]
public sealed class VehicleProfileController : SparePartsControllerBase
{
    private readonly UserVehicleService _service;
    private readonly VinDecodeService _vinDecodeService;

    public VehicleProfileController(UserVehicleService service, VinDecodeService vinDecodeService)
    {
        _service = service;
        _vinDecodeService = vinDecodeService;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserVehicleDto>> GetAll()
        => Ok(_service.GetAllForUser(CurrentUserId));

    [HttpGet("{id:int}")]
    public ActionResult<UserVehicleDto> GetById(int id)
    {
        var vehicle = _service.GetById(id);
        if (vehicle is null) return NotFound();
        return Ok(vehicle);
    }

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateUserVehicleRequest request)
        => Ok(_service.Create(request, CurrentUserId, CurrentUserId));

    [HttpPut("{id:int}/set-default")]
    public IActionResult SetDefault(int id)
    {
        _service.SetDefault(id, CurrentUserId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _service.Deactivate(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("vin-decode")]
    public ActionResult<VinDecodeResult> DecodeVin([FromQuery] string vin)
        => Ok(_vinDecodeService.Decode(vin));
}
