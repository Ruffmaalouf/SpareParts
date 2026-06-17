using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.ApiPlatform;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/api-platform")]
[Authorize]
public sealed class ApiPlatformController : SparePartsControllerBase
{
    private readonly ApiPlatformService _service;

    public ApiPlatformController(ApiPlatformService service)
    {
        _service = service;
    }

    [HttpGet("keys")]
    public IActionResult GetKeys()
        => Ok(_service.GetKeys());

    [HttpPost("keys")]
    public ActionResult<ApiKeyCreatedDto> Create([FromBody] CreateApiKeyRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpDelete("keys/{keyId:int}")]
    public IActionResult Revoke(int keyId)
    {
        _service.Revoke(keyId);
        return NoContent();
    }
}
