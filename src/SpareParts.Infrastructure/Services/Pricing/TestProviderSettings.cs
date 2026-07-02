namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class TestProviderSettings
{
    /// <summary>When true, the test provider's checkout always reports a failed payment. Useful for QA.</summary>
    public bool ForceFailure { get; init; }
}
