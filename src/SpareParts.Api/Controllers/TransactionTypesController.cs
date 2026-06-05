using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/transactiontypes")]
    [Authorize]
    public class TransactionTypesController : ControllerBase
    {
        private readonly TransactionTypesService _service;

        public TransactionTypesController(TransactionTypesService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<TransactionTypeDto>> GetAll()
            => Ok(_service.GetAll());

        [HttpPost]
        public IActionResult Create([FromBody] CreateTransactionTypeRequest req)
        {
            _service.Create(req);
            return NoContent();
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateTransactionTypeRequest req)
        {
            _service.Update(id, req);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
