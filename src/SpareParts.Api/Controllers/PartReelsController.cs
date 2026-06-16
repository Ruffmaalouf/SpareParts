using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/part-reels")]
[Authorize]
public sealed class PartReelsController : SparePartsControllerBase
{
    private readonly PartReelsService _service;

    public PartReelsController(PartReelsService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PartReelDto>> GetAll(
        [FromQuery] PartReelStatus? status = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var pagination = NormalizeOptionalPagination(page, pageSize);
        var result = _service.GetAll(pagination.Page, pagination.PageSize, status);
        if (pagination.IsPaged)
            ApplyPaginationHeaders(pagination.Page, pagination.PageSize, result.TotalCount);
        return Ok(result.Items);
    }

    [HttpGet("{id:int}")]
    public ActionResult<PartReelDto> GetById(int id)
    {
        var reel = _service.GetById(id);
        if (reel is null) return NotFound();
        return Ok(reel);
    }

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreatePartReelRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}/view")]
    public IActionResult IncrementView(int id)
    {
        _service.IncrementView(id);
        return NoContent();
    }

    [HttpPut("{id:int}/like")]
    public IActionResult IncrementLike(int id)
    {
        _service.IncrementLike(id);
        return NoContent();
    }

    [HttpPut("{id:int}/archive")]
    public IActionResult Archive(int id)
    {
        _service.Archive(id, CurrentUserId);
        return NoContent();
    }
}
