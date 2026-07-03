using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Api.Services;

/// <summary>Thin adapter over <see cref="IHostEnvironment"/> so Infrastructure-layer services can check
/// whether they are running in Development without depending on Microsoft.Extensions.Hosting.Abstractions.</summary>
public sealed class HostRuntimeEnvironment : IRuntimeEnvironment
{
    private readonly IHostEnvironment _hostEnvironment;

    public HostRuntimeEnvironment(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public bool IsDevelopment => _hostEnvironment.IsDevelopment();
}
