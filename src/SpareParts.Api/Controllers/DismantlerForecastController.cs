using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Forecast;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/dismantler-forecast")]
[Authorize]
public sealed class DismantlerForecastController : SparePartsControllerBase
{
    private readonly DismantlerForecastService _service;

    public DismantlerForecastController(DismantlerForecastService service)
    {
        _service = service;
    }

    [HttpPost]
    public ActionResult<DismantlerForecastResult> Forecast([FromBody] DismantlerForecastRequest request)
        => Ok(_service.Forecast(request));
}
