using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Accounting;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/accounts")]
    [Authorize]
    public class AccountsController : SparePartsControllerBase
    {
        private readonly AccountingService _service;

        public AccountsController(AccountingService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<AccountDto>> GetAll()
            => Ok(_service.GetAccounts());

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public ActionResult<int> Create([FromBody] CreateAccountRequest request)
            => Ok(_service.CreateAccount(request, CurrentUserId));

        [HttpPut("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult Update(int id, [FromBody] CreateAccountRequest request)
        {
            _service.UpdateAccount(id, request, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult Delete(int id)
        {
            _service.DeleteAccount(id);
            return NoContent();
        }
    }
}
