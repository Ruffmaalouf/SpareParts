using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Auth;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly UsersService _service;

        public UsersController(UsersService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<UserDto>> GetAll() => Ok(_service.GetAll());

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateUserRequest req) => Ok(_service.Create(req));

        [HttpPut("{id:int}")]
        public ActionResult Update(int id, [FromBody] UpdateUserRequest req)
        {
            _service.Update(id, req);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public ActionResult Deactivate(int id)
        {
            _service.Deactivate(id);
            return NoContent();
        }
    }
}
