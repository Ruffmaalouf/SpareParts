using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SpareParts.Api.Services;

/// <summary>
/// Computes weak ETags for cached composed read models (Ignition Report 04 §08):
/// derived from the scope identity (tenant, date/client) plus a content hash of the
/// serialized aggregate, but emitted as an opaque SHA-256 digest so no tenant id,
/// cache key, or internal identifier is recoverable from the header value.
/// </summary>
public static class ApiResponseETagFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Compute<T>(string scope, T payload)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes($"{scope}|{json}"));
        return $"W/\"{Convert.ToHexString(digest, 0, 16).ToLowerInvariant()}\"";
    }
}
