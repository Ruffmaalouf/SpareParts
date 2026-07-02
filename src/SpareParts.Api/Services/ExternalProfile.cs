namespace SpareParts.Api.Services;

/// <summary>Normalized profile returned by an external identity provider (Google/Facebook) after verification.</summary>
internal sealed record ExternalProfile(string Provider, string ProviderUserId, string? Email, string FullName);
