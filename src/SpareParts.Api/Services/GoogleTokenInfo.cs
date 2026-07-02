using System.Text.Json.Serialization;

namespace SpareParts.Api.Services;

/// <summary>Response payload from Google's tokeninfo endpoint, used to verify a Google external login id token.</summary>
internal sealed class GoogleTokenInfo
{
    [JsonPropertyName("aud")]
    public string Audience { get; set; } = string.Empty;

    [JsonPropertyName("sub")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public string EmailVerified { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
