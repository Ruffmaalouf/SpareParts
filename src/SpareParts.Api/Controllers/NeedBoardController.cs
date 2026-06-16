using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.NeedBoard;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/needboard")]
[Authorize]
public sealed class NeedBoardController : SparePartsControllerBase
{
    private readonly NeedBoardService _service;

    public NeedBoardController(NeedBoardService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PartWantedAdDto>> GetAll(
        [FromQuery] PartWantedAdStatus? status = null,
        [FromQuery] string? vehicleMake = null,
        [FromQuery] string? vehicleModel = null,
        [FromQuery] string? search = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var pagination = NormalizeOptionalPagination(page, pageSize, maxPageSize: 200);
        var result = _service.GetAll(pagination.Page, pagination.PageSize, status, vehicleMake, vehicleModel, search);
        if (pagination.IsPaged)
            ApplyPaginationHeaders(pagination.Page, pagination.PageSize, result.TotalCount);
        return Ok(result.Items);
    }

    [HttpGet("{id:int}")]
    public ActionResult<PartWantedAdDto> GetById(int id)
    {
        var ad = _service.GetById(id);
        if (ad is null) return NotFound();
        return Ok(ad);
    }

    [HttpGet("{id:int}/offers")]
    public ActionResult<IEnumerable<PartWantedAdOfferDto>> GetOffers(int id)
        => Ok(_service.GetOffersForAd(id));

    [HttpPost]
    public ActionResult<int> CreateAd([FromBody] CreatePartWantedAdRequest request)
        => Ok(_service.CreateAd(request, CurrentUserId, CurrentUserId));

    [HttpPost("{id:int}/offers")]
    public ActionResult<int> CreateOffer(int id, [FromBody] CreatePartWantedAdOfferRequest request)
    {
        request.WantedAdId = id;
        return Ok(_service.CreateOffer(request, CurrentUserId, CurrentUserId));
    }

    [HttpPut("{id:int}/offers/{offerId:int}/accept")]
    public IActionResult AcceptOffer(int id, int offerId)
    {
        _service.AcceptOffer(offerId, id, CurrentUserId);
        return NoContent();
    }

    [HttpPut("{id:int}/cancel")]
    public IActionResult Cancel(int id)
    {
        _service.CancelAd(id, CurrentUserId);
        return NoContent();
    }
}
