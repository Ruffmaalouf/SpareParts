using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public ActionResult<int> Create([FromBody] CreateAccountRequest request)
            => Ok(_service.CreateAccount(request, CurrentUserId));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateAccountRequest request)
        {
            _service.UpdateAccount(id, request, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            _service.DeleteAccount(id);
            return NoContent();
        }
    }
}
