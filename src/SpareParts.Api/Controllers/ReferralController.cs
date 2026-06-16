using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Referral;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/referral")]
[Authorize]
public sealed class ReferralController : SparePartsControllerBase
{
    private readonly ReferralService _service;

    public ReferralController(ReferralService service)
    {
        _service = service;
    }

    [HttpGet("my-code")]
    public ActionResult<ReferralCodeDto> GetMyCode()
    {
        var code = _service.GetMyCode(CurrentUserId);
        if (code is null) return NotFound();
        return Ok(code);
    }

    [HttpPost("generate")]
    public ActionResult<ReferralCodeDto> GenerateCode()
        => Ok(_service.GenerateCode(CurrentUserId, CurrentUserId));

    [HttpGet("my-referrals")]
    public ActionResult<IEnumerable<ReferralSignupDto>> GetMyReferrals()
    {
        var code = _service.GetMyCode(CurrentUserId);
        if (code is null) return Ok(Enumerable.Empty<ReferralSignupDto>());
        return Ok(_service.GetReferrals(code.Id));
    }
}
