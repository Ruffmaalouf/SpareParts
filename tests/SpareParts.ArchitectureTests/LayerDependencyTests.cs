using System.Reflection;
using SpareParts.Application.Management.Currencies;
using SpareParts.Api.Controllers;
using SpareParts.Domain.Common;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class LayerDependencyTests
{
    [Fact]
    public void Domain_ShouldNotReference_OuterLayers()
    {
        AssertDoesNotReference(
            typeof(Entity).Assembly,
            "SpareParts.Infrastructure",
            "SpareParts.Api",
            "SpareParts.Desktop");
    }

    [Fact]
    public void InfrastructureInterfaces_ShouldNotReference_InfrastructureApiOrDesktop()
    {
        AssertDoesNotReference(
            typeof(ISqlConnectionFactory).Assembly,
            "SpareParts.Infrastructure",
            "SpareParts.Api",
            "SpareParts.Desktop");
    }

    [Fact]
    public void Application_ShouldNotReference_InfrastructureApiOrDesktop()
    {
        AssertDoesNotReference(
            typeof(CurrencyRateProjectionService).Assembly,
            "SpareParts.Infrastructure",
            "SpareParts.Api",
            "SpareParts.Desktop");
    }

    [Fact]
    public void Infrastructure_ShouldNotReference_ApiOrDesktop()
    {
        AssertDoesNotReference(
            typeof(CreateSaleHandler).Assembly,
            "SpareParts.Api",
            "SpareParts.Desktop");
    }

    [Fact]
    public void Api_ShouldNotReference_DesktopAssemblies()
    {
        AssertDoesNotReference(
            typeof(AuthController).Assembly,
            "SpareParts.Desktop");
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenPrefixes)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        var forbiddenReference = references.FirstOrDefault(reference =>
            forbiddenPrefixes.Any(prefix => reference.StartsWith(prefix, StringComparison.Ordinal)));

        Assert.True(
            forbiddenReference is null,
            $"{assembly.GetName().Name} should not reference {forbiddenReference ?? "the forbidden layer"}.");
    }
}
