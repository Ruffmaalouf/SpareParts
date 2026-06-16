using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/marketplace")]
[Authorize]
public sealed class WhatsAppLinkController : SparePartsControllerBase
{
    private readonly WhatsAppLinkService _service;

    public WhatsAppLinkController(WhatsAppLinkService service)
    {
        _service = service;
    }

    [HttpGet("whatsapp-link")]
    public IActionResult GetWhatsAppLink([FromQuery] int partId, [FromQuery] string? sellerPhone = null)
    {
        var (link, message) = _service.GeneratePartLink(partId, sellerPhone, CurrentTenantId);
        return Ok(new { link, message });
    }

    [HttpGet("catalog-export")]
    public ActionResult<IEnumerable<WhatsAppCatalogItemDto>> ExportCatalog()
        => Ok(_service.ExportCatalog(CurrentTenantId));
}
