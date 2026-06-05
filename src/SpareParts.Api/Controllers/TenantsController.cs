using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public TenantsController(TenantsService service)
    {
        _service = service;
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
    public ActionResult<int> Create([FromBody] CreateTenantRequest req) =>
        Ok(_service.Create(req));

    [HttpPut("{id:int}")]
    public ActionResult Update(int id, [FromBody] UpdateTenantRequest req)
    {
        _service.Update(id, req);
        return NoContent();
    }
}
