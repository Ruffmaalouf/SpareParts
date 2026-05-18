namespace SpareParts.Desktop.Abstractions.Dialogs;

public interface IUserNotificationService
{
    void Show(string message, string title, NotificationKind kind = NotificationKind.Info);
}
