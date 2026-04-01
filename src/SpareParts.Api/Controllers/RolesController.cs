using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        [Authorize(Roles = "Admin")]
        public ActionResult<RoleDto> Create([FromBody] CreateRoleRequest req) => Ok(_service.Create(req));

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public ActionResult Update(int id, [FromBody] UpdateRoleRequest req)
        {
            _service.Update(id, req);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }

        [HttpGet("{id:int}/menu-access")]
        public ActionResult<IEnumerable<RoleMenuAccessDto>> GetMenuAccessByRoleId(int id)
            => Ok(_service.GetMenuAccessByRoleId(id));

        [HttpGet("by-name/{roleName}/menu-access")]
        public ActionResult<IEnumerable<RoleMenuAccessDto>> GetMenuAccessByRoleName(string roleName)
            => Ok(_service.GetMenuAccessByRoleName(roleName));

        [HttpPut("{id:int}/menu-access")]
        [Authorize(Roles = "Admin")]
        public ActionResult UpdateMenuAccess(int id, [FromBody] UpdateRoleMenuAccessRequest req)
        {
            _service.UpdateMenuAccess(id, req);
            return NoContent();
        }
    }
}
