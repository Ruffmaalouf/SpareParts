using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MechanicDesk;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/repair-orders")]
[Authorize]
public sealed class RepairOrdersController : SparePartsControllerBase
{
    private readonly RepairOrdersService _service;

    public RepairOrdersController(RepairOrdersService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<RepairOrderDto>> GetAll(
        [FromQuery] RepairOrderStatus? status = null,
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
    public ActionResult<RepairOrderDto> GetById(int id)
    {
        var order = _service.GetById(id);
        if (order is null) return NotFound();
        return Ok(order);
    }

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateRepairOrderRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}/send")]
    public IActionResult Send(int id)
    {
        _service.Send(id, CurrentUserId);
        return NoContent();
    }

    [HttpPost("items/{itemId:int}/quotes")]
    public ActionResult<int> AddQuote(int itemId, [FromBody] CreateRepairOrderQuoteRequest request)
    {
        request.RepairOrderItemId = itemId;
        return Ok(_service.AddQuote(request, CurrentUserId));
    }

    [HttpPut("items/{itemId:int}/quotes/{quoteId:int}/accept")]
    public IActionResult AcceptQuote(int itemId, int quoteId)
    {
        _service.AcceptQuote(quoteId, CurrentUserId);
        return NoContent();
    }

    [HttpPut("{id:int}/cancel")]
    public IActionResult Cancel(int id)
    {
        _service.Cancel(id, CurrentUserId);
        return NoContent();
    }
}
