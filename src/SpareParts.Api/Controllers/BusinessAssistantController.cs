using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessAssistant;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/business-assistant")]
    [Authorize]
    public sealed class BusinessAssistantController : SparePartsControllerBase
    {
        private readonly BusinessAssistantService _service;

        public BusinessAssistantController(BusinessAssistantService service)
        {
            _service = service;
        }

        [HttpPost("ask")]
        public ActionResult<BusinessAssistantResponseDto> Ask([FromBody] BusinessAssistantQueryRequest request)
            => Ok(_service.Ask(request.Question));

        [HttpPost("actions/run")]
        public ActionResult<BusinessAssistantResponseDto> RunAction([FromBody] BusinessAssistantActionRequest request)
            => Ok(_service.RunAction(request));
    }
}
