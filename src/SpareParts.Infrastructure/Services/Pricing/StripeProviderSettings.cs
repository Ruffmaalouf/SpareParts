namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class StripeProviderSettings
{
    public bool Enabled { get; init; }
    public string PublishableKey { get; init; } = string.Empty;

    /// <summary>Resolved from environment/user-secrets only — never stored in appsettings.json.</summary>
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}
