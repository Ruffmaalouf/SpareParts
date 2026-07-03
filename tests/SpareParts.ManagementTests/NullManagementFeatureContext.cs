using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.Parts;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Management;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.ManagementTests;

/// <summary>
/// No-op context for tests that only need ViewModel data properties
/// and never exercise coordinator/callback behavior.
/// </summary>
internal sealed class NullManagementFeatureContext : IManagementFeatureContext
{
    public static readonly NullManagementFeatureContext Instance = new();

    public ManagementCoordinator Coordinator => throw new InvalidOperationException("Coordinator not available in test context.");
    public ICommand? ImportTableCommand => null;
    public Task RefreshAsync() => Task.CompletedTask;
    public void SetStatus(string message, bool success) { }
    public string GetDefaultCurrencyCode() => "USD";
}

internal sealed class NullFilePickerService : IFilePickerService
{
    public IReadOnlyList<string> PickFiles(FilePickerRequest request) => [];
}

internal sealed class NullUserNotificationService : IUserNotificationService
{
    public void Show(string message, string title, NotificationKind kind = NotificationKind.Info) { }
    public bool Confirm(string message, string title) => true;
}

internal sealed class NullPartWorkspaceService : IPartWorkspaceService
{
    public void OpenListingPackage(PartWorkspaceRequest request) { }
}
