using System.Text.Json.Serialization;

namespace SpareParts.Api.Services;

/// <summary>Response payload from Facebook's <c>/me</c> endpoint, used to read the verified user's profile.</summary>
internal sealed class FacebookProfileResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
