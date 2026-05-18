using System.Linq;
using System.Windows;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.UsedCars;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.Management;

namespace SpareParts.Desktop.Wpf.Services.UsedCars;

public sealed class UsedCarWorkspaceService : IUsedCarWorkspaceService
{
    private readonly ManagementCoordinator _coordinator;
    private readonly IFilePickerService _filePickerService;
    private readonly IUserNotificationService _notificationService;

    public UsedCarWorkspaceService(
        ICrudApiClient crudApi,
        ICarCatalogApiClient carCatalogApi,
        IPartsApiClient partsApi,
        IFilePickerService filePickerService,
        IUserNotificationService notificationService)
    {
        _coordinator = new ManagementCoordinator(crudApi, carCatalogApi, partsApi);
        _filePickerService = filePickerService;
        _notificationService = notificationService;
    }

    public void OpenGallery(UsedCarWorkspaceRequest request)
    {
        var window = new UsedCarGalleryWindow(
            _coordinator,
            request,
            _filePickerService,
            _notificationService);

        ShowDialog(window);
    }

    public void OpenParts(UsedCarWorkspaceRequest request)
    {
        var window = new UsedCarPartsWindow(
            _coordinator,
            request,
            _filePickerService,
            _notificationService);

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
