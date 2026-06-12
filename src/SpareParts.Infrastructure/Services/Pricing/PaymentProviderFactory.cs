using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class PaymentProviderFactory : IPaymentProviderFactory
{
    private readonly IReadOnlyDictionary<string, IPaymentProvider> _providers;
    private readonly PaymentSettings _settings;

    public PaymentProviderFactory(IEnumerable<IPaymentProvider> providers, PaymentSettings settings)
    {
        _providers = providers.ToDictionary(p => p.ProviderCode, StringComparer.OrdinalIgnoreCase);
        _settings = settings;
    }

    public IPaymentProvider GetProvider(string providerCode)
    {
        if (string.IsNullOrWhiteSpace(providerCode) || !_providers.TryGetValue(providerCode, out var provider))
        {
            throw new ValidationException($"Unknown or unavailable payment provider '{providerCode}'.");
        }

        return provider;
    }

    public IPaymentProvider GetDefaultProvider() => GetProvider(_settings.DefaultProvider);
}
