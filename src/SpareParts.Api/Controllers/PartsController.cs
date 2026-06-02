using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SpareParts.Api.Notifications;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/parts")]
    [Authorize]
    public class PartsController : SparePartsControllerBase
    {
        private readonly PartsService _service;
        private readonly IHubContext<NotificationsHub> _notifications;
        private readonly ILogger<PartsController> _logger;

        public PartsController(
            PartsService service,
            IHubContext<NotificationsHub> notifications,
            ILogger<PartsController> logger)
        {
            _service = service;
            _notifications = notifications;
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<IEnumerable<PartDto>> GetAll(
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] int? usedCarId = null)
        {
            var pagination = NormalizeOptionalPagination(page, pageSize, maxPageSize: 5000);
            var result = _service.GetAll(pagination.Page, pagination.PageSize, usedCarId);
            if (pagination.IsPaged)
            {
                ApplyPaginationHeaders(pagination.Page, pagination.PageSize, result.TotalCount);
            }

            return Ok(result.Items);
        }

        [HttpGet("dead-stock")]
        public ActionResult<DeadStockReportDto> GetDeadStock(
            [FromQuery] int minDormantDays = 90,
            [FromQuery] int take = 25)
            => Ok(_service.GetDeadStock(minDormantDays, take));

        [HttpGet("{id:int}/stock")]
        public ActionResult<IReadOnlyList<PartStockDto>> GetStock(int id)
            => Ok(_service.GetStockByWarehouse(id));

        [HttpPost]
        public async Task<ActionResult<int>> Create(
            [FromBody] CreatePartRequest req,
            CancellationToken cancellationToken)
        {
            var id = _service.Create(req, CurrentUserId);
            await NotifyPartAddedAsync(id, req, cancellationToken);
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreatePartRequest req)
        {
            _service.Update(id, req, CurrentUserId);
            return NoContent();
        }

        [HttpPut("{id:int}/usedcar")]
        public IActionResult UpdateUsedCar(int id, [FromBody] UpdatePartUsedCarRequest req)
        {
            _service.UpdateUsedCar(id, req.UsedCarId, CurrentUserId);
            return NoContent();
        }

        [HttpPost("{id:int}/transfer")]
        public IActionResult TransferStock(int id, [FromBody] TransferPartRequest req)
        {
            _service.TransferStock(id, req, CurrentUserId);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }

        private async Task NotifyPartAddedAsync(
            int id,
            CreatePartRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await _notifications.Clients.All.SendAsync(
                    NotificationEvents.PartAdded,
                    PartAddedNotification.From(id, request),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Part {PartId} was saved, but the live notification could not be delivered.", id);
            }
        }

        [HttpPost("ai/notes")]
        public async Task<ActionResult<GeneratePartNotesResponse>> GenerateNotes(
            [FromBody] GeneratePartNotesRequest request,
            CancellationToken cancellationToken)
            => Ok(await _service.GenerateNotesAsync(request, cancellationToken));
    }
}
