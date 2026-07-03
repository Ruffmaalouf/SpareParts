using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Tenants;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public class TenantsController : ControllerBase
{
    private readonly TenantsService _service;
    private readonly ILogger<TenantsController> _logger;

    public TenantsController(TenantsService service, ILogger<TenantsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TenantDto>> GetAll() =>
        Ok(_service.GetAll());

    [HttpGet("{id:int}")]
    public ActionResult<TenantDto> GetById(int id)
    {
        var tenant = _service.GetById(id);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateTenantRequest req)
    {
        var id = _service.Create(req);

        _logger.LogInformation(
            "SuperAdmin created tenant {TenantId}. ByUserId={UserId} TraceId={TraceId}",
            id, CurrentUserId, HttpContext.TraceIdentifier);

        return Ok(id);
    }

    [HttpPut("{id:int}")]
    public ActionResult Update(int id, [FromBody] UpdateTenantRequest req)
    {
        _service.Update(id, req);

        _logger.LogInformation(
            "SuperAdmin updated tenant {TenantId}. ByUserId={UserId} TraceId={TraceId}",
            id, CurrentUserId, HttpContext.TraceIdentifier);

        return NoContent();
    }

    private int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
