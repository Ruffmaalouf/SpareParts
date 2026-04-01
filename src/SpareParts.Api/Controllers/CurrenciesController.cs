using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    [Authorize]
    public class CurrenciesController : ControllerBase
    {
        private readonly CurrenciesService _service;

        public CurrenciesController(CurrenciesService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CurrencyRateDto>> GetAll() => Ok(_service.GetAll());
    }
}
