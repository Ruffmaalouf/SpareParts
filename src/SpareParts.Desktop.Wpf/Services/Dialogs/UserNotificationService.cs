using SpareParts.Desktop.Abstractions.Dialogs;

namespace SpareParts.Desktop.Wpf.Services.Dialogs;

public sealed class UserNotificationService : IUserNotificationService
{
    public void Show(string message, string title, NotificationKind kind = NotificationKind.Info)
        => CustomMessageBox.Show(message, title, ToVisualKind(kind));

    private static string ToVisualKind(NotificationKind kind)
        => kind switch
        {
            NotificationKind.Success => "Success",
            NotificationKind.Warning => "Warning",
            NotificationKind.Error => "Error",
            _ => "Info"
        };
}
