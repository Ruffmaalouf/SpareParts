using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/half-cuts")]
[Authorize]
public sealed class HalfCutController : SparePartsControllerBase
{
    private readonly HalfCutService _service;

    public HalfCutController(HalfCutService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<HalfCutListingDto>> GetAll(
        [FromQuery] HalfCutStatus? status = null,
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
    public ActionResult<HalfCutListingDto> GetById(int id)
    {
        var listing = _service.GetById(id);
        if (listing is null) return NotFound();
        return Ok(listing);
    }

    [HttpGet("{id:int}/claims")]
    public ActionResult<IEnumerable<HalfCutPartClaimDto>> GetClaims(int id)
        => Ok(_service.GetClaims(id));

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateHalfCutListingRequest request)
        => Ok(_service.CreateListing(request, CurrentUserId));

    [HttpPost("{id:int}/claims")]
    public ActionResult<int> AddClaim(int id, [FromBody] CreateHalfCutPartClaimRequest request)
    {
        request.HalfCutListingId = id;
        return Ok(_service.CreateClaim(request, CurrentUserId));
    }

    [HttpPut("{id:int}/claims/{claimId:int}/confirm")]
    public IActionResult ConfirmClaim(int id, int claimId)
    {
        _service.ConfirmClaim(claimId, CurrentUserId);
        return NoContent();
    }

    [HttpPut("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromQuery] int status)
    {
        _service.UpdateStatus(id, (HalfCutStatus)status, CurrentUserId);
        return NoContent();
    }
}
