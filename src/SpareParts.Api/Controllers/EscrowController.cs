using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/escrow")]
[Authorize]
public sealed class EscrowController : SparePartsControllerBase
{
    private readonly EscrowService _service;

    public EscrowController(EscrowService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<EscrowTransactionDto>> GetAll(
        [FromQuery] EscrowStatus? status = null,
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
    public ActionResult<EscrowTransactionDto> GetById(int id)
    {
        var tx = _service.GetById(id);
        if (tx is null) return NotFound();
        return Ok(tx);
    }

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateEscrowRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] UpdateEscrowStatusRequest request)
    {
        _service.UpdateStatus(id, request, CurrentUserId);
        return NoContent();
    }
}
