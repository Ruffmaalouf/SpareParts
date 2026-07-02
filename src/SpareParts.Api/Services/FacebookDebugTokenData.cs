using System.Text.Json.Serialization;

namespace SpareParts.Api.Services;

/// <summary>The <c>data</c> object nested inside a <see cref="FacebookDebugTokenResponse"/>.</summary>
internal sealed class FacebookDebugTokenData
{
    [JsonPropertyName("app_id")]
    public string AppId { get; set; } = string.Empty;

    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}
