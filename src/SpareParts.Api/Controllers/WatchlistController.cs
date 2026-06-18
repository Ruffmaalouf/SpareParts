using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Watchlist;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/watchlist")]
[Authorize]
public sealed class WatchlistController : SparePartsControllerBase
{
    private readonly WatchlistService _service;

    public WatchlistController(WatchlistService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<WatchedPartDto>> GetAll()
        => Ok(_service.GetAllForUser(CurrentUserId));

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateWatchedPartRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpGet("{id:int}/matches")]
    public ActionResult<int> GetMatchCount(int id)
        => Ok(_service.GetMatchCount(id, CurrentUserId));

    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        _service.Deactivate(id, CurrentUserId);
        return NoContent();
    }
}
