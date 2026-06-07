using System.Linq;
using System.Windows;
using SpareParts.Desktop.Abstractions.Parts;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.Management;

namespace SpareParts.Desktop.Wpf.Services.Inventory;

public sealed class PartWorkspaceService : IPartWorkspaceService
{
    private readonly ManagementCoordinator _coordinator;

    public PartWorkspaceService(ICrudApiClient crudApi, ICarCatalogApiClient carCatalogApi, IPartsApiClient partsApi)
    {
        _coordinator = new ManagementCoordinator(crudApi, carCatalogApi, partsApi);
    }

    public void OpenListingPackage(PartWorkspaceRequest request)
    {
        var window = new PartListingWindow(_coordinator, request);
        ShowDialog(window);
    }

    private static void ShowDialog(Window window)
    {
        var owner = System.Windows.Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(candidate => candidate.IsActive);

        if (owner != null && owner != window)
        {
            window.Owner = owner;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }

        window.ShowDialog();
    }
}
