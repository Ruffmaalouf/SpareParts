namespace SpareParts.Infrastructure.Interfaces;

/// <summary>
/// Minimal, dependency-free view of the hosting environment for Infrastructure-layer services that need
/// to know whether they are running in Development (e.g. to gate test-only payment providers) without
/// taking a direct dependency on Microsoft.Extensions.Hosting.Abstractions.
/// </summary>
public interface IRuntimeEnvironment
{
    bool IsDevelopment { get; }
}
