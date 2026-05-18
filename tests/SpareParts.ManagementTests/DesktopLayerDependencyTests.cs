using System.Reflection;
using SpareParts.Application.Management.Currencies;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.ViewModels;
using Xunit;

namespace SpareParts.ManagementTests;

public sealed class DesktopLayerDependencyTests
{
    [Fact]
    public void DesktopInterfaces_ShouldNotReference_HelpersViewModelsControlsOrShell()
    {
        AssertDoesNotReference(
            typeof(IAccountingApiClient).Assembly,
            "SpareParts.Desktop.Helpers",
            "SpareParts.Desktop.ViewModels",
            "SpareParts.Desktop.Controls",
            "SpareParts.Desktop.Wpf");
    }

    [Fact]
    public void DesktopAbstractions_ShouldNotReference_OtherDesktopLayers()
    {
        AssertDoesNotReference(
            typeof(IUserNotificationService).Assembly,
            "SpareParts.Desktop.Helpers",
            "SpareParts.Desktop.ViewModels",
            "SpareParts.Desktop.Controls",
            "SpareParts.Desktop.Wpf");
    }

    [Fact]
    public void DesktopHelpers_ShouldNotReference_ViewModelsOrShell()
    {
        AssertDoesNotReference(
            typeof(ThemeManager).Assembly,
            "SpareParts.Desktop.ViewModels",
            "SpareParts.Desktop.Wpf");
    }

    [Fact]
    public void DesktopControls_ShouldNotReference_ViewModelsOrShell()
    {
        AssertDoesNotReference(
            typeof(RoleItem).Assembly,
            "SpareParts.Desktop.ViewModels",
            "SpareParts.Desktop.Wpf");
    }

    [Fact]
    public void DesktopViewModels_ShouldNotReference_ShellAssembly()
    {
        AssertDoesNotReference(
            typeof(ManagementViewModel).Assembly,
            "SpareParts.Desktop.Wpf");
    }

    [Fact]
    public void DesktopViewModels_ShouldReference_AbstractionsAssembly()
    {
        var references = typeof(ManagementViewModel).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains("SpareParts.Desktop.Abstractions", references);
    }

    [Fact]
    public void DesktopViewModels_ShouldReference_ApplicationAssembly()
    {
        var references = typeof(ManagementViewModel).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains(typeof(CurrencyRateProjectionService).Assembly.GetName().Name, references);
    }

    [Fact]
    public void DesktopShell_ShouldReference_ViewModelsAssembly()
    {
        var references = typeof(MainWindow).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();

        Assert.Contains("SpareParts.Desktop.ViewModels", references);
    }

    private static void AssertDoesNotReference(Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();

        var forbiddenReference = references.FirstOrDefault(reference =>
            forbiddenAssemblyNames.Any(forbidden => string.Equals(reference, forbidden, StringComparison.Ordinal)));

        Assert.True(
            forbiddenReference is null,
            $"{assembly.GetName().Name} should not reference {forbiddenReference ?? "the forbidden layer"}.");
    }
}
