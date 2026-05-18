using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Communications;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/communications")]
    [Authorize]
    public sealed class CommunicationsController : SparePartsControllerBase
    {
        private const string WebhookSecretHeader = "X-SpareParts-Communication-Secret";
        private readonly CommunicationsService _service;
        private readonly WhatsAppCampaignService _campaigns;
        private readonly CommunicationOptions _options;

        public CommunicationsController(
            CommunicationsService service,
            WhatsAppCampaignService campaigns,
            CommunicationOptions options)
        {
            _service = service;
            _campaigns = campaigns;
            _options = options;
        }

        [HttpPost("send")]
        public async Task<ActionResult<SendBusinessMessageResponse>> Send(
            [FromBody] SendBusinessMessageRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _service.SendAsync(request, CurrentUserId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("recent")]
        public ActionResult<IReadOnlyList<OutboundMessageLogDto>> GetRecent([FromQuery] int take = 50)
            => Ok(_service.GetRecent(take));

        [HttpGet("conversations")]
        public ActionResult<IReadOnlyList<CommunicationConversationDto>> GetConversations(
            [FromQuery] string? search = null,
            [FromQuery] int take = 100)
            => Ok(_service.GetConversations(search, take));

        [HttpGet("messages")]
        public ActionResult<IReadOnlyList<CommunicationConversationMessageDto>> GetMessages(
            [FromQuery] string phone,
            [FromQuery] int take = 200)
            => Ok(_service.GetConversationMessages(phone, take));

        [HttpGet("campaign-assets")]
        public ActionResult<IReadOnlyList<WhatsAppCampaignAssetDto>> GetCampaignAssets()
            => Ok(_campaigns.GetAssets());

        [HttpPost("campaign-preview")]
        public ActionResult<WhatsAppCampaignPreviewResponse> PreviewCampaign(
            [FromBody] WhatsAppCampaignPreviewRequest request)
            => Ok(_campaigns.Preview(request));

        [HttpPost("campaigns/send")]
        public async Task<ActionResult<WhatsAppCampaignSendResponse>> SendCampaign(
            [FromBody] SendWhatsAppCampaignRequest request,
            CancellationToken cancellationToken)
        {
            var response = await _campaigns.SendAsync(request, CurrentUserId, cancellationToken);
            return Ok(response);
        }

        [HttpGet("campaigns/recent")]
        public ActionResult<IReadOnlyList<WhatsAppCampaignSummaryDto>> GetRecentCampaigns([FromQuery] int take = 30)
            => Ok(_campaigns.GetRecent(take));

        [HttpPost("inbound")]
        [AllowAnonymous]
        public ActionResult<CommunicationConversationMessageDto> RecordInbound(
            [FromBody] RecordInboundMessageRequest request)
        {
            if (!IsValidInboundSecret())
            {
                return Unauthorized();
            }

            return Ok(_service.RecordInbound(request));
        }

        private bool IsValidInboundSecret()
        {
            if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                return false;
            }

            return Request.Headers.TryGetValue(WebhookSecretHeader, out var submittedSecret)
                && string.Equals(submittedSecret.ToString(), _options.WebhookSecret, StringComparison.Ordinal);
        }
    }
}
