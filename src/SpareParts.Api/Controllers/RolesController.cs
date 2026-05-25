using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly RolesService _service;

        public RolesController(RolesService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<RoleDto>> GetAll() => Ok(_service.GetAll());

        [HttpGet("{id:int}")]
        public ActionResult<RoleDto> GetById(int id) => Ok(_service.GetById(id));

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        public ActionResult<RoleDto> Create([FromBody] CreateRoleRequest req) => Ok(_service.Create(req));

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        public ActionResult Update(int id, [FromBody] UpdateRoleRequest req)
        {
            _service.Update(id, req);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        public ActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }

        [HttpGet("{id:int}/menu-access")]
        public ActionResult<IEnumerable<RoleMenuAccessDto>> GetMenuAccessByRoleId(int id)
            => Ok(_service.GetMenuAccessByRoleId(id));

        [HttpPut("{id:int}/menu-access")]
        [Authorize(Policy = AuthorizationPolicies.Admin)]
        public ActionResult UpdateMenuAccess(int id, [FromBody] UpdateRoleMenuAccessRequest req)
        {
            _service.UpdateMenuAccess(id, req);
            return NoContent();
        }
    }
}
