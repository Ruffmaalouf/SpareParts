using System.Text.Json.Serialization;

namespace SpareParts.Api.Services;

/// <summary>Response payload from Facebook's debug_token endpoint, used to verify a Facebook external login access token.</summary>
internal sealed class FacebookDebugTokenResponse
{
    [JsonPropertyName("data")]
    public FacebookDebugTokenData? Data { get; set; }
}
