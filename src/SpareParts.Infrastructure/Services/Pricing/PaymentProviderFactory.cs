using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers;
    private readonly PaymentSettings _settings;
    private readonly IRuntimeEnvironment _runtimeEnvironment;

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers, PaymentSettings settings, IRuntimeEnvironment runtimeEnvironment)
    {
        _providers = providers.ToDictionary(p => p.ProviderCode, StringComparer.OrdinalIgnoreCase);
        _settings = settings;
        _runtimeEnvironment = runtimeEnvironment;
    }

    public IPaymentProvider GetProvider(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode) || !_providers.TryGetValue(providerCode, out var provider))
        {
            throw new ValidationException($"Unknown or unavailable payment provider '{providerCode}'.");
        }

        EnsureProviderIsUsable(provider);

        return provider;
    }

    public IPaymentProvider GetDefaultProvider() => GetProvider(_settings.DefaultProvider);

    /// <summary>
    /// The Test provider's webhook accepts an unauthenticated JSON body and marks any payment as
    /// succeeded/failed with no signature verification — it must never be reachable outside Development,
    /// even if a client supplies "TEST" as the {provider} route segment on the public webhook endpoint.
    /// The Stripe provider already gates itself on <see cref="StripeProviderSettings.Enabled"/> and a
    /// configured secret/webhook key (see StripePaymentProvider), and the Manual provider's webhook
    /// unconditionally reports failure (payments are only completed via the authenticated admin
    /// mark-paid action), so neither needs an additional environment gate here.
    /// </summary>
    private void EnsureProviderIsUsable(IPaymentProvider provider)
    {
        if (string.Equals(provider.ProviderCode, PaymentProviderCode.Test, StringComparison.OrdinalIgnoreCase)
            && !_runtimeEnvironment.IsDevelopment)
        {
            throw new ValidationException($"Payment provider '{provider.ProviderCode}' is not available in this environment.");
        }
    }
}
